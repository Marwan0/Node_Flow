/*
 Photoshop ExtendScript
 Exports every layer (including hidden) as PNG files + layout.json for Unity import.
 Layout contains hierarchical nodes with parent IDs and sibling order.
 Includes Fast Mode for quick iteration.
*/

#target photoshop
app.bringToFront();

if (app.documents.length === 0) {
    alert("Open a PSD document first.");
} else {
    var originalDoc = app.activeDocument;
    var outputFolder = Folder.selectDialog("Select export folder for PNG + layout.json");
    if (outputFolder) {
        var runtimeOptions = askRuntimeOptions();
        var layout = {
            version: 2,
            document: {
                width: px(originalDoc.width),
                height: px(originalDoc.height),
                name: originalDoc.name
            },
            nodes: [],
            layers: []
        };

        var counterObj = { value: 0 };
        var fastWorkDoc = null;
        if (runtimeOptions.fastMode) {
            fastWorkDoc = originalDoc.duplicate("TEMP_FAST_SOURCE", false);
            makeBackgroundEditable(fastWorkDoc);
        }

        exportLayerCollection(originalDoc, fastWorkDoc, originalDoc.layers, "", "", layout, outputFolder, counterObj, runtimeOptions);

        if (fastWorkDoc) {
            try { fastWorkDoc.close(SaveOptions.DONOTSAVECHANGES); } catch (closeFastErr) {}
        }

        writeLayoutJson(outputFolder, layout);
        alert(
            "Export finished.\n" +
            "Mode: " + (runtimeOptions.fastMode ? "Fast" : "Accurate") + "\n" +
            "Nodes: " + layout.nodes.length + "\n" +
            "Exported PNGs: " + counterObj.value + "\n" +
            "Folder: " + outputFolder.fsName
        );
    }
}

function exportLayerCollection(originalDoc, fastWorkDoc, layers, parentId, parentPath, layout, outputFolder, counterObj, runtimeOptions) {
    if (!layers || layers.length === 0) {
        return;
    }

    // Bottom to top for predictable draw ordering in Unity.
    for (var i = layers.length - 1; i >= 0; i--) {
        var layer = layers[i];
        if (!runtimeOptions.exportHidden && !safeVisible(layer)) {
            continue;
        }

        var nodeId = parentPath ? (parentPath + "/" + i) : String(i);

        var bounds = getLayerBoundsSafe(layer);
        var isGroup = layer.typename === "LayerSet";
        var isArtLayer = layer.typename === "ArtLayer";
        var isText = false;
        var textValue = "";

        if (isArtLayer) {
            try {
                isText = layer.kind === LayerKind.TEXT;
                if (isText && layer.textItem) {
                    textValue = layer.textItem.contents || "";
                }
            } catch (e) {}
        }

        var fileName = "";
        var finalX = bounds.x;
        var finalY = bounds.y;
        var finalWidth = bounds.width;
        var finalHeight = bounds.height;
        if (isArtLayer && bounds.width > 0 && bounds.height > 0) {
            var exportResult = exportSingleLayerPngByPath(
                originalDoc,
                fastWorkDoc,
                nodeId,
                outputFolder,
                counterObj,
                layer.name,
                runtimeOptions
            );
            if (exportResult) {
                fileName = exportResult.file || "";
                if (fileName) {
                    finalX = exportResult.x;
                    finalY = exportResult.y;
                    finalWidth = exportResult.width;
                    finalHeight = exportResult.height;
                }
            }
        }

        var node = {
            id: nodeId,
            parentId: parentId,
            name: safeString(layer.name),
            file: fileName,
            x: finalX,
            y: finalY,
            width: finalWidth,
            height: finalHeight,
            opacity: safeOpacity(layer),
            visible: safeVisible(layer),
            isText: isText,
            text: textValue,
            isGroup: isGroup,
            order: i
        };

        layout.nodes.push(node);

        if (!isGroup) {
            layout.layers.push({
                name: node.name,
                file: node.file,
                x: node.x,
                y: node.y,
                width: node.width,
                height: node.height,
                opacity: node.opacity,
                visible: node.visible,
                isText: node.isText,
                text: node.text
            });
        }

        if (isGroup) {
            exportLayerCollection(originalDoc, fastWorkDoc, layer.layers, nodeId, nodeId, layout, outputFolder, counterObj, runtimeOptions);
        }
    }
}

function exportSingleLayerPngByPath(originalDoc, fastWorkDoc, layerPath, outputFolder, counterObj, sourceLayerName, runtimeOptions) {
    var fileName = buildFileName(layerPath, sourceLayerName, counterObj.value);
    counterObj.value++;

    if (runtimeOptions.fastMode) {
        return exportSingleLayerFast(fastWorkDoc, layerPath, outputFolder, fileName, runtimeOptions);
    }

    return exportSingleLayerAccurate(originalDoc, layerPath, outputFolder, fileName);
}

function exportSingleLayerAccurate(originalDoc, layerPath, outputFolder, fileName) {
    var outputFile = new File(outputFolder.fsName + "/" + fileName);
    var tempDoc = originalDoc.duplicate("TEMP_EXPORT_" + fileName, false);

    try {
        makeBackgroundEditable(tempDoc);
        hideAllLayers(tempDoc.layers);

        var tempLayer = findLayerByPath(tempDoc, layerPath);
        if (!tempLayer) {
            tempDoc.close(SaveOptions.DONOTSAVECHANGES);
            return "";
        }

        showParentChain(tempLayer);
        showRequiredClippingChain(tempLayer);
        tempLayer.visible = true;

        var renderedLayer = null;
        try {
            renderedLayer = tempDoc.mergeVisibleLayers();
        } catch (mergeErr) {
            renderedLayer = tempLayer;
        }

        var renderBounds = getLayerBoundsSafe(renderedLayer);
        if (renderBounds.width <= 0 || renderBounds.height <= 0) {
            tempDoc.close(SaveOptions.DONOTSAVECHANGES);
            return null;
        }

        try {
            tempDoc.crop([
                UnitValue(renderBounds.x, "px"),
                UnitValue(renderBounds.y, "px"),
                UnitValue(renderBounds.x + renderBounds.width, "px"),
                UnitValue(renderBounds.y + renderBounds.height, "px")
            ]);
        } catch (cropErr) {}

        var options = new ExportOptionsSaveForWeb();
        options.format = SaveDocumentType.PNG;
        options.PNG8 = false;
        options.transparency = true;
        options.interlaced = false;
        options.quality = 100;

        tempDoc.exportDocument(outputFile, ExportType.SAVEFORWEB, options);
        tempDoc.close(SaveOptions.DONOTSAVECHANGES);

        if (!outputFile.exists) {
            return null;
        }

        return {
            file: fileName,
            x: renderBounds.x,
            y: renderBounds.y,
            width: renderBounds.width,
            height: renderBounds.height
        };
    } catch (err) {
        try { tempDoc.close(SaveOptions.DONOTSAVECHANGES); } catch (closeErr) {}
        return null;
    }
}

function exportSingleLayerFast(fastWorkDoc, layerPath, outputFolder, fileName, runtimeOptions) {
    if (!fastWorkDoc) {
        return null;
    }

    try {
        hideAllLayers(fastWorkDoc.layers);
        var tempLayer = findLayerByPath(fastWorkDoc, layerPath);
        if (!tempLayer) {
            return null;
        }

        showParentChain(tempLayer);
        if (runtimeOptions.fastIncludeClipping) {
            showRequiredClippingChain(tempLayer);
        }
        tempLayer.visible = true;

        var renderDoc = fastWorkDoc.duplicate("TEMP_FAST_RENDER_" + fileName, true);
        return exportRenderDocToPng(renderDoc, outputFolder, fileName);
    } catch (err) {
        return null;
    }
}

function exportRenderDocToPng(renderDoc, outputFolder, fileName) {
    var outputFile = new File(outputFolder.fsName + "/" + fileName);

    try {
        makeBackgroundEditable(renderDoc);

        var renderedLayer = null;
        try { renderedLayer = renderDoc.activeLayer; } catch (e) {}
        if (!renderedLayer && renderDoc.layers && renderDoc.layers.length > 0) {
            renderedLayer = renderDoc.layers[0];
        }

        var renderBounds = getLayerBoundsSafe(renderedLayer);
        if (renderBounds.width <= 0 || renderBounds.height <= 0) {
            renderDoc.close(SaveOptions.DONOTSAVECHANGES);
            return null;
        }

        try {
            renderDoc.crop([
                UnitValue(renderBounds.x, "px"),
                UnitValue(renderBounds.y, "px"),
                UnitValue(renderBounds.x + renderBounds.width, "px"),
                UnitValue(renderBounds.y + renderBounds.height, "px")
            ]);
        } catch (cropErr) {}

        var options = new ExportOptionsSaveForWeb();
        options.format = SaveDocumentType.PNG;
        options.PNG8 = false;
        options.transparency = true;
        options.interlaced = false;
        options.quality = 100;

        renderDoc.exportDocument(outputFile, ExportType.SAVEFORWEB, options);
        renderDoc.close(SaveOptions.DONOTSAVECHANGES);

        if (!outputFile.exists) {
            return null;
        }

        return {
            file: fileName,
            x: renderBounds.x,
            y: renderBounds.y,
            width: renderBounds.width,
            height: renderBounds.height
        };
    } catch (err) {
        try { renderDoc.close(SaveOptions.DONOTSAVECHANGES); } catch (closeErr) {}
        return null;
    }
}

function getLayerBoundsSafe(layer) {
    try {
        var b = layer.bounds;
        var left = px(b[0]);
        var top = px(b[1]);
        var right = px(b[2]);
        var bottom = px(b[3]);
        return {
            x: left,
            y: top,
            width: Math.max(0, right - left),
            height: Math.max(0, bottom - top)
        };
    } catch (e) {
        return { x: 0, y: 0, width: 0, height: 0 };
    }
}

function safeOpacity(layer) {
    try {
        return layer.opacity / 100.0;
    } catch (e) {
        return 1.0;
    }
}

function safeVisible(layer) {
    try {
        return !!layer.visible;
    } catch (e) {
        return true;
    }
}

function safeString(value) {
    if (value === undefined || value === null) {
        return "";
    }
    return String(value);
}

function askRuntimeOptions() {
    var fastMode = confirm(
        "Enable Fast Mode?\n\n" +
        "Yes = faster iteration (reuses temp doc, still exports hidden layers)\n" +
        "No = accurate mode (slower, safest for final export)"
    );

    var exportHidden = true;
    var fastIncludeClipping = false;

    if (fastMode) {
        fastIncludeClipping = confirm(
            "Fast Mode: include clipping-chain reconstruction?\n\n" +
            "Yes = better clipped-layer fidelity, slower\n" +
            "No = faster export"
        );
    }

    return {
        fastMode: fastMode,
        exportHidden: exportHidden,
        fastIncludeClipping: fastIncludeClipping
    };
}

function makeBackgroundEditable(doc) {
    try {
        if (doc.backgroundLayer) {
            doc.activeLayer = doc.backgroundLayer;
            doc.activeLayer.isBackgroundLayer = false;
        }
    } catch (e) {}
}

function hideAllLayers(layers) {
    if (!layers) return;

    for (var i = 0; i < layers.length; i++) {
        var layer = layers[i];
        try { layer.visible = false; } catch (e) {}
        if (layer.typename === "LayerSet") {
            hideAllLayers(layer.layers);
        }
    }
}

function showParentChain(layer) {
    var current = layer;
    while (current) {
        try { current.visible = true; } catch (e) {}
        if (!current.parent || current.parent.typename === "Document") {
            break;
        }
        current = current.parent;
    }
}

function showRequiredClippingChain(layer) {
    var current = layer;
    while (current) {
        var isClipped = false;
        try { isClipped = !!current.grouped; } catch (e) {}
        if (!isClipped) {
            break;
        }

        var baseLayer = findLayerBelow(current);
        if (!baseLayer) {
            break;
        }

        showParentChain(baseLayer);
        try { baseLayer.visible = true; } catch (setVisibleErr) {}
        current = baseLayer;
    }
}

function findLayerBelow(layer) {
    if (!layer || !layer.parent || !layer.parent.layers) {
        return null;
    }

    var siblings = layer.parent.layers;
    for (var i = 0; i < siblings.length; i++) {
        if (siblings[i] === layer) {
            var belowIndex = i + 1;
            if (belowIndex < siblings.length) {
                return siblings[belowIndex];
            }
            return null;
        }
    }

    return null;
}

function findLayerByPath(doc, path) {
    if (!doc || !path) {
        return null;
    }

    var parts = path.split("/");
    var siblings = doc.layers;
    var current = null;

    for (var i = 0; i < parts.length; i++) {
        var index = parseInt(parts[i], 10);
        if (isNaN(index) || !siblings || index < 0 || index >= siblings.length) {
            return null;
        }

        current = siblings[index];
        siblings = current.layers;
    }

    return current;
}

function buildFileName(layerPath, layerName, counter) {
    var pathPart = layerPath.replace(/[\\\/]/g, "-");
    var namePart = toAsciiSlug(layerName);
    return "L_" + pathPart + "_" + namePart + "_" + pad(counter, 4) + ".png";
}

function toAsciiSlug(value) {
    if (!value) {
        return "layer";
    }

    var text = String(value);
    text = text.replace(/[^\x20-\x7E]/g, "_");
    text = text.replace(/[\\\/:\*\?"<>\|]/g, "_");
    text = text.replace(/\s+/g, "_");
    text = text.replace(/_+/g, "_");
    text = text.replace(/^_+|_+$/g, "");

    if (text.length === 0) {
        text = "layer";
    }

    if (text.length > 48) {
        text = text.substring(0, 48);
    }

    return text;
}

function writeLayoutJson(folder, layoutObj) {
    var file = new File(folder.fsName + "/layout.json");
    file.encoding = "UTF8";
    file.open("w");
    file.write(stringify(layoutObj, 2));
    file.close();
}

function stringify(value, indentSize) {
    var indentUnit = "  ";
    if (indentSize === 4) indentUnit = "    ";
    return stringifyValue(value, "", indentUnit);
}

function stringifyValue(value, currentIndent, indentUnit) {
    if (value === null) return "null";

    var t = typeof value;
    if (t === "string") return quoteString(value);
    if (t === "number") return isFinite(value) ? String(value) : "0";
    if (t === "boolean") return value ? "true" : "false";

    if (value.constructor === Array) {
        if (value.length === 0) return "[]";
        var arrNextIndent = currentIndent + indentUnit;
        var arrOut = "[\n";
        for (var i = 0; i < value.length; i++) {
            arrOut += arrNextIndent + stringifyValue(value[i], arrNextIndent, indentUnit);
            if (i < value.length - 1) arrOut += ",";
            arrOut += "\n";
        }
        arrOut += currentIndent + "]";
        return arrOut;
    }

    var keys = [];
    for (var key in value) {
        if (value.hasOwnProperty(key)) keys.push(key);
    }
    if (keys.length === 0) return "{}";

    var objNextIndent = currentIndent + indentUnit;
    var out = "{\n";
    for (var k = 0; k < keys.length; k++) {
        var name = keys[k];
        out += objNextIndent + quoteString(name) + ": " + stringifyValue(value[name], objNextIndent, indentUnit);
        if (k < keys.length - 1) out += ",";
        out += "\n";
    }
    out += currentIndent + "}";
    return out;
}

function quoteString(s) {
    var escaped = s;
    escaped = escaped.replace(/\\/g, "\\\\");
    escaped = escaped.replace(/"/g, "\\\"");
    escaped = escaped.replace(/\r/g, "\\r");
    escaped = escaped.replace(/\n/g, "\\n");
    escaped = escaped.replace(/\t/g, "\\t");
    return "\"" + escaped + "\"";
}

function pad(num, size) {
    var s = String(num);
    while (s.length < size) s = "0" + s;
    return s;
}

function px(unitValue) {
    return Number(unitValue.as("px"));
}

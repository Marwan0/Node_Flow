/*
 Photoshop ExtendScript – OPTIMIZED
 Exports every layer (including hidden) as PNG files + layout.json for Unity import.
 Layout contains hierarchical nodes with parent IDs and sibling order.

 Performance optimizations over original:
   1. Single working copy + History-state undo (no per-layer document duplication)
   2. SaveAs PNG instead of slow SaveForWeb
   3. Early skip for zero-bounds layers
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

        // ── Create ONE working copy for the entire export ──
        var workDoc = originalDoc.duplicate("TEMP_EXPORT_WORK", false);
        makeBackgroundEditable(workDoc);

        // Save the clean starting state so we can always revert
        var cleanHistoryState = workDoc.activeHistoryState;

        exportLayerCollection(
            originalDoc, workDoc, originalDoc.layers,
            "", "", layout, outputFolder, counterObj, runtimeOptions,
            cleanHistoryState
        );

        try { workDoc.close(SaveOptions.DONOTSAVECHANGES); } catch (closErr) {}

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

// =====================================================================
//  LAYER TREE WALKER
// =====================================================================

function exportLayerCollection(originalDoc, workDoc, layers, parentId, parentPath, layout, outputFolder, counterObj, runtimeOptions, cleanHistoryState) {
    if (!layers || layers.length === 0) return;

    for (var i = layers.length - 1; i >= 0; i--) {
        var layer = layers[i];
        if (!runtimeOptions.exportHidden && !safeVisible(layer)) continue;

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

        // Skip layers with zero bounds early
        if (isArtLayer && bounds.width > 0 && bounds.height > 0) {
            var exportResult = exportSingleLayer(
                workDoc, nodeId, outputFolder, counterObj,
                layer.name, runtimeOptions, cleanHistoryState
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
            exportLayerCollection(
                originalDoc, workDoc, layer.layers, nodeId, nodeId,
                layout, outputFolder, counterObj, runtimeOptions,
                cleanHistoryState
            );
        }
    }
}

// =====================================================================
//  SINGLE-LAYER EXPORT  (no per-layer document duplication!)
// =====================================================================

function exportSingleLayer(workDoc, layerPath, outputFolder, counterObj, sourceLayerName, runtimeOptions, cleanHistoryState) {
    var fileName = buildFileName(layerPath, sourceLayerName, counterObj.value);
    counterObj.value++;
    var outputFile = new File(outputFolder.fsName + "/" + fileName);

    try {
        // 1. Revert to clean state (all layers restored from original dup)
        workDoc.activeHistoryState = cleanHistoryState;

        // 2. Hide ALL layers (reliable DOM walk - guaranteed to work)
        hideAllLayers(workDoc.layers);

        // 3. Show only target layer + parent chain
        var targetLayer = findLayerByPath(workDoc, layerPath);
        if (!targetLayer) return null;

        showParentChain(targetLayer);
        targetLayer.visible = true;

        // 4. Optionally include clipping chain (Accurate mode always does)
        if (!runtimeOptions.fastMode || runtimeOptions.fastIncludeClipping) {
            showRequiredClippingChain(targetLayer);
        }

        // 5. Merge visible layers to render effects / clipping masks
        //    mergeVisibleLayers fails if only 1 visible layer - that is fine,
        //    a single layer does not need merging, just use it directly.
        //    We suppress Photoshop dialogs so the error doesn't show a popup.
        var renderedLayer = targetLayer;
        var savedDialogMode = app.displayDialogs;
        app.displayDialogs = DialogModes.NO;
        try {
            renderedLayer = workDoc.mergeVisibleLayers();
        } catch (mergeErr) {
            // Single visible layer - use it directly, no merge needed
        }
        app.displayDialogs = savedDialogMode;

        var renderBounds = getLayerBoundsSafe(renderedLayer);
        if (renderBounds.width <= 0 || renderBounds.height <= 0) {
            workDoc.activeHistoryState = cleanHistoryState;
            return null;
        }

        // 6. Crop to content
        try {
            workDoc.crop([
                UnitValue(renderBounds.x, "px"),
                UnitValue(renderBounds.y, "px"),
                UnitValue(renderBounds.x + renderBounds.width, "px"),
                UnitValue(renderBounds.y + renderBounds.height, "px")
            ]);
        } catch (cropErr) {}

        // 7. Save as PNG (fast SaveAs, not slow SaveForWeb)
        savePngFast(workDoc, outputFile);

        // 8. Revert to clean state for next layer
        workDoc.activeHistoryState = cleanHistoryState;

        if (!outputFile.exists) return null;

        return {
            file: fileName,
            x: renderBounds.x,
            y: renderBounds.y,
            width: renderBounds.width,
            height: renderBounds.height
        };
    } catch (err) {
        try { workDoc.activeHistoryState = cleanHistoryState; } catch (revertErr) {}
        return null;
    }
}

// =====================================================================
//  HIDE ALL LAYERS - reliable DOM-based recursive walk
// =====================================================================

function hideAllLayers(layers) {
    if (!layers) return;
    for (var i = 0; i < layers.length; i++) {
        try { layers[i].visible = false; } catch (e) {}
        if (layers[i].typename === "LayerSet") {
            hideAllLayers(layers[i].layers);
        }
    }
}

// =====================================================================
//  FAST PNG SAVE - SaveAs instead of SaveForWeb
// =====================================================================

function savePngFast(doc, outputFile) {
    var pngOpts = new PNGSaveOptions();
    pngOpts.compression = 6;
    pngOpts.interlaced = false;
    doc.saveAs(outputFile, pngOpts, true, Extension.LOWERCASE);
}

// =====================================================================
//  LAYER UTILITIES
// =====================================================================

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
    try { return layer.opacity / 100.0; } catch (e) { return 1.0; }
}

function safeVisible(layer) {
    try { return !!layer.visible; } catch (e) { return true; }
}

function safeString(value) {
    if (value === undefined || value === null) return "";
    return String(value);
}

function makeBackgroundEditable(doc) {
    try {
        if (doc.backgroundLayer) {
            doc.activeLayer = doc.backgroundLayer;
            doc.activeLayer.isBackgroundLayer = false;
        }
    } catch (e) {}
}

function showParentChain(layer) {
    var current = layer;
    while (current) {
        try { current.visible = true; } catch (e) {}
        if (!current.parent || current.parent.typename === "Document") break;
        current = current.parent;
    }
}

function showRequiredClippingChain(layer) {
    var current = layer;
    while (current) {
        var isClipped = false;
        try { isClipped = !!current.grouped; } catch (e) {}
        if (!isClipped) break;

        var baseLayer = findLayerBelow(current);
        if (!baseLayer) break;

        showParentChain(baseLayer);
        try { baseLayer.visible = true; } catch (setVisErr) {}
        current = baseLayer;
    }
}

function findLayerBelow(layer) {
    if (!layer || !layer.parent || !layer.parent.layers) return null;
    var siblings = layer.parent.layers;
    for (var i = 0; i < siblings.length; i++) {
        if (siblings[i] === layer) {
            var belowIndex = i + 1;
            if (belowIndex < siblings.length) return siblings[belowIndex];
            return null;
        }
    }
    return null;
}

function findLayerByPath(doc, path) {
    if (!doc || !path) return null;
    var parts = path.split("/");
    var siblings = doc.layers;
    var current = null;

    for (var i = 0; i < parts.length; i++) {
        var index = parseInt(parts[i], 10);
        if (isNaN(index) || !siblings || index < 0 || index >= siblings.length) return null;
        current = siblings[index];
        siblings = current.layers;
    }
    return current;
}

// =====================================================================
//  FILE NAME & OUTPUT HELPERS
// =====================================================================

function buildFileName(layerPath, layerName, counter) {
    var pathPart = layerPath.split("/").join("-").split("\\").join("-");
    var namePart = toAsciiSlug(layerName);
    return "L_" + pathPart + "_" + namePart + "_" + pad(counter, 4) + ".png";
}

function toAsciiSlug(value) {
    if (!value) return "layer";
    var text = String(value);
    text = text.replace(new RegExp("[^\\x20-\\x7E]", "g"), "_");
    text = text.replace(new RegExp("[\\\\/:*?\"<>|]", "g"), "_");
    text = text.replace(new RegExp("\\s+", "g"), "_");
    text = text.replace(new RegExp("_+", "g"), "_");
    text = text.replace(new RegExp("^_+|_+$", "g"), "");
    if (text.length === 0) text = "layer";
    if (text.length > 48) text = text.substring(0, 48);
    return text;
}

function askRuntimeOptions() {
    var fastMode = confirm(
        "Enable Fast Mode?\n\n" +
        "Yes = faster (skips clipping chain reconstruction)\n" +
        "No = accurate (includes clipping chains, slightly slower)"
    );

    var exportHidden = true;
    var fastIncludeClipping = false;

    if (fastMode) {
        fastIncludeClipping = confirm(
            "Fast Mode: include clipping-chain reconstruction?\n\n" +
            "Yes = better clipped-layer fidelity, slightly slower\n" +
            "No = fastest export"
        );
    }

    return {
        fastMode: fastMode,
        exportHidden: exportHidden,
        fastIncludeClipping: fastIncludeClipping
    };
}

// =====================================================================
//  JSON WRITER
// =====================================================================

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
        var arrNext = currentIndent + indentUnit;
        var arrOut = "[\n";
        for (var i = 0; i < value.length; i++) {
            arrOut += arrNext + stringifyValue(value[i], arrNext, indentUnit);
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

    var objNext = currentIndent + indentUnit;
    var out = "{\n";
    for (var k = 0; k < keys.length; k++) {
        var name = keys[k];
        out += objNext + quoteString(name) + ": " + stringifyValue(value[name], objNext, indentUnit);
        if (k < keys.length - 1) out += ",";
        out += "\n";
    }
    out += currentIndent + "}";
    return out;
}

function quoteString(s) {
    var escaped = String(s);
    escaped = escaped.split("\\").join("\\\\");
    escaped = escaped.split("\"").join("\\\"");
    escaped = escaped.split("\r").join("\\r");
    escaped = escaped.split("\n").join("\\n");
    escaped = escaped.split("\t").join("\\t");
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

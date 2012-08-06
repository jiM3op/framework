﻿"use strict";

SF.registerModule("Lines", function () {

    (function ($) {
        $.widget("SF.baseLine", {

            options: {
                prefix: "",
                onEntityChanged: null
            },

            keys: {
                entity: "sfEntity"
            },

            runtimeInfo: function () {
                return new SF.RuntimeInfo(this.options.prefix);
            },

            staticInfo: function () {
                return SF.StaticInfo(this.options.prefix);
            },

            pf: function (s) {
                return "#" + SF.compose(this.options.prefix, s);
            },

            checkValidation: function (validatorOptions) {
                if (typeof validatorOptions == "undefined" || typeof validatorOptions.type == "undefined") {
                    throw "validatorOptions.type must be supplied to checkValidation";
                }

                var info = this.runtimeInfo();
                $.extend(validatorOptions, {
                    prefix: this.options.prefix,
                    id: (info.find().length !== 0) ? info.id() : ''
                });
                var validator = new SF.PartialValidator(validatorOptions);
                var result = validator.validate();
                if (!result.isValid) {
                    if (!confirm(lang.signum.popupErrors)) return false;
                    validator.showErrors(result.modelState, true);
                }
                this.updateLinks(result.newToStr, result.newLink);
                return true;
            },

            updateLinks: function (newToStr, newLink) {
                //Abstract function
            },

            fireOnEntityChanged: function (hasEntity) {
                this.updateButtonsDisplay(hasEntity);
                if (!SF.isEmpty(this.options.onEntityChanged)) {
                    this.options.onEntityChanged();
                }
            },

            remove: function () {
                $(this.pf(SF.Keys.toStr)).val("").removeClass(SF.Validator.inputErrorClass);
                $(this.pf(SF.Keys.link)).val("").html("").removeClass(SF.Validator.inputErrorClass);
                this.runtimeInfo().removeEntity();

                this.removeSpecific();
                this.fireOnEntityChanged(false);
            },

            getRuntimeType: function (_onTypeFound) {
                var types = this.staticInfo().types().split(",");
                if (types.length == 1) {
                    return _onTypeFound(types[0]);
                }

                SF.openTypeChooser(this.options.prefix, _onTypeFound);
            },

            create: function (_viewOptions) {
                var _self = this;
                var type = this.getRuntimeType(function (type) {
                    _self.typedCreate($.extend({ type: type }, _viewOptions));
                });
            },

            typedCreate: function (_viewOptions) {
                if (SF.isEmpty(_viewOptions.type)) {
                    throw "ViewOptions type parameter must not be null in baseLine typedCreate. Call create instead";
                }
                if (_viewOptions.navigate) {
                    window.open(_viewOptions.controllerUrl.substring(0, _viewOptions.controllerUrl.lastIndexOf("/") + 1) + _viewOptions.type, "_blank");
                    return;
                }
                var viewOptions = this.viewOptionsForCreating(_viewOptions);
                var template = this.getEmbeddedTemplate(viewOptions);
                if (!SF.isEmpty(template)) {
                    new SF.ViewNavigator(viewOptions).showCreateOk(template);
                }
                else {
                    new SF.ViewNavigator(viewOptions).createOk();
                }
            },

            getEmbeddedTemplate: function (viewOptions) {
                return window[SF.compose(this.options.prefix, "sfTemplate")];
            },

            find: function (_findOptions) {
                var _self = this;
                var type = this.getRuntimeType(function (type) {
                    _self.typedFind($.extend({ webQueryName: type }, _findOptions));
                });
            },

            typedFind: function (_findOptions) {
                if (SF.isEmpty(_findOptions.webQueryName)) {
                    throw "FindOptions webQueryName parameter must not be null in EBaseline typedFind. Call find instead";
                }
                var findOptions = this.createFindOptions(_findOptions);
                SF.FindNavigator.openFinder(findOptions);
            },

            extraJsonParams: function (_prefix) {
                var extraParams = {};

                var staticInfo = this.staticInfo();

                //If Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
                if (staticInfo.isEmbedded()) {
                    var pathInfo = SF.fullPathNodesSelector(this.options.prefix);
                    for (var i = 0, l = pathInfo.length; i < l; i++) {
                        var node = pathInfo[i];
                        extraParams[node.id] = node.value;
                    }
                }

                if (staticInfo.isReadOnly()) {
                    extraParams.readOnly = true;
                }

                return extraParams;
            },

            updateButtonsDisplay: function (hasEntity) {
                var btnCreate = $(this.pf("btnCreate"));
                var btnRemove = $(this.pf("btnRemove"));
                var btnFind = $(this.pf("btnFind"));
                var btnView = $(this.pf("btnView"));
                var link = $(this.pf(SF.Keys.link));
                var txt = $(this.pf(SF.Keys.toStr));

                if (hasEntity == true) {
                    if (link.html() == "")
                        link.html("&nbsp;");
                    if (link.length > 0) {
                        txt.hide();
                        link.show();
                    }
                    else
                        txt.show();
                    btnCreate.hide(); btnFind.hide();
                    btnRemove.show(); btnView.show();
                }
                else {
                    if (link.length > 0) {
                        link.hide();
                        txt.show();
                    }
                    else
                        txt.hide();
                    btnRemove.hide(); btnView.hide();
                    btnCreate.show(); btnFind.show();
                }
            }
        });

        $.widget("SF.entityLine", $.SF.baseLine, {

            options: {},

            updateLinks: function (newToStr, newLink) {
                var link = $(this.pf(SF.Keys.link));
                link.html(newToStr);
                if (link.filter('a').length !== 0)
                    link.attr('href', newLink);
                $(this.pf(SF.Keys.toStr)).val('');
            },

            view: function (_viewOptions) {
                var viewOptions = this.viewOptionsForViewing(_viewOptions);
                new SF.ViewNavigator(viewOptions).viewOk();
            },

            viewOptionsForViewing: function (_viewOptions) {
                var self = this;
                var info = this.runtimeInfo();
                return $.extend({
                    containerDiv: SF.compose(this.options.prefix, self.keys.entity),
                    onOk: function () { return self.onViewingOk(_viewOptions.validationOptions); },
                    onOkClosed: function () { self.fireOnEntityChanged(true); },
                    type: info.runtimeType(),
                    id: info.id(),
                    prefix: this.options.prefix,
                    requestExtraJsonData: this.extraJsonParams()
                }, _viewOptions);
            },

            onViewingOk: function (validatorOptions) {
                var valOptions = $.extend(validatorOptions || {}, {
                    type: this.runtimeInfo().runtimeType()
                });
                var acceptChanges = this.checkValidation(valOptions);
                return acceptChanges;
            },

            viewOptionsForCreating: function (_viewOptions) {
                var self = this;
                return $.extend({
                    onOk: function (clonedElements) { return self.onCreatingOk(clonedElements, _viewOptions.validationOptions, _viewOptions.type); },
                    onOkClosed: function () { self.fireOnEntityChanged(true); },
                    prefix: this.options.prefix,
                    requestExtraJsonData: this.extraJsonParams()
                }, _viewOptions);
            },

            newEntity: function (clonedElements, runtimeType) {
                var info = this.runtimeInfo();
                info.setEntity(runtimeType, '');
                info.find().after(SF.hiddenDiv(SF.compose(this.options.prefix, this.keys.entity), ""));
                $(this.pf(this.keys.entity)).append(clonedElements);
            },

            onCreatingOk: function (clonedElements, validatorOptions, runtimeType) {
                var valOptions = $.extend(validatorOptions || {}, {
                    type: runtimeType
                });
                var acceptChanges = this.checkValidation(valOptions);
                if (acceptChanges) {
                    this.newEntity(clonedElements, runtimeType);
                }
                return acceptChanges;
            },

            createFindOptions: function (_findOptions) {
                var self = this;
                return $.extend({
                    prefix: this.options.prefix,
                    onOk: function (selectedItems) { return self.onFindingOk(selectedItems); },
                    onOkClosed: function () { self.fireOnEntityChanged(true); },
                    allowMultiple: false
                }, _findOptions);
            },

            onFindingOk: function (selectedItems) {
                if (selectedItems == null || selectedItems.length != 1) {
                    throw "No item or more than one item was returned from Find Window";
                }
                var info = this.runtimeInfo();
                info.setEntity(selectedItems[0].type, selectedItems[0].id);
                if ($(this.pf(this.keys.entity)).length == 0)
                    info.find().after(SF.hiddenDiv(SF.compose(this.options.prefix, this.keys.entity), ""));
                $(this.pf(SF.Keys.toStr)).val(''); //Clean
                $(this.pf(SF.Keys.link)).html(selectedItems[0].toStr).attr('href', selectedItems[0].link);
                return true;
            },

            onAutocompleteSelected: function (controlId, data) {
                var selectedItems = [{
                    id: data.id,
                    type: data.type,
                    toStr: $('#' + controlId).val(),
                    link: ""
                }];
                this.onFindingOk(selectedItems);
                this.fireOnEntityChanged(true);
            },

            entityAutocomplete: function ($elem, options) {
                var lastXhr; //To avoid previous requests results to be shown
                $elem.autocomplete({
                    delay: options.delay || 200,
                    source: function (request, response) {
                        if (lastXhr)
                            lastXhr.abort();
                        lastXhr = $.ajax({
                            url: options.url,
                            data: { types: options.types, l: options.count || 5, q: request.term },
                            success: function (data) {
                                lastXhr = null;
                                response($.map(data, function (item) {
                                    return {
                                        label: item.text,
                                        value: item
                                    }
                                }));
                            }
                        });
                    },
                    focus: function (event, ui) {
                        $elem.val(ui.item.value.text);
                        return false;
                    },
                    select: function (event, ui) {
                        var controlId = $elem.attr("id");
                        var prefix = controlId.substr(0, controlId.indexOf(SF.Keys.toStr) - 1);
                        $("#" + prefix).data("entityLine").onAutocompleteSelected(controlId, ui.item.value);
                    }
                });
            },

            removeSpecific: function () {
                $(this.pf(this.keys.entity)).remove();
            }
        });

        $.widget("SF.entityCombo", $.SF.entityLine, {

            options: {},

            keys: {
                entity: "sfEntity",
                combo: "sfCombo"
            },

            updateLinks: function (newToStr, newLink) {
                $("#" + this.options.prefix + " option:selected").html(newToStr);
            },

            selectedValue: function () {
                var selected = $(this.pf(this.keys.combo) + " > option:selected");
                if (selected.length === 0) {
                    return null;
                }
                var fullValue = selected.val();
                var separator = fullValue.indexOf(";");
                var value = [];
                if (separator === -1) {
                    value.runtimeType = this.staticInfo().singleType();
                    value.id = fullValue;
                }
                else {
                    value.runtimeType = fullValue.substring(0, separator);
                    value.id = fullValue.substring(separator + 1, fullValue.length);
                }
                return value;
            },

            setSelected: function () {
                var newValue = this.selectedValue(),
                    newRuntimeType = "",
                    newId = "",
                    newEntity = newValue !== null && !SF.isEmpty(newValue.id);

                if (newEntity) {
                    newRuntimeType = newValue.runtimeType;
                    newId = newValue.id;
                }
                var runtimeInfo = this.runtimeInfo();
                runtimeInfo.setEntity(newRuntimeType, newId);
                $(this.pf(this.keys.entity)).html(''); //Clean
                this.fireOnEntityChanged(newEntity);
            },

            view: function (_viewOptions) {
                var viewOptions = this.viewOptionsForViewing(_viewOptions);
                if (viewOptions.navigate) {
                    var runtimeInfo = this.runtimeInfo();
                    if (!SF.isEmpty(runtimeInfo.id())) {
                        window.open(viewOptions.controllerUrl.substring(0, viewOptions.controllerUrl.lastIndexOf("/") + 1) + runtimeInfo.runtimeType() + "/" + runtimeInfo.id(), "_blank");
                    }
                }
                else {
                    new SF.ViewNavigator(viewOptions).viewOk();
                }
            }
        });

        $.widget("SF.entityLineDetail", $.SF.baseLine, {

            options: {}, //baseLine Options + detailDiv

            typedCreate: function (_viewOptions) {
                if (SF.isEmpty(_viewOptions.type)) {
                    throw "ViewOptions type parameter must not be null in entityLineDetail typedCreate. Call create instead";
                }
                var viewOptions = this.viewOptionsForCreating(_viewOptions);
                var template = this.getEmbeddedTemplate(viewOptions);
                if (!SF.isEmpty(template)) { //Template pre-loaded: EmbeddedEntity
                    $('#' + viewOptions.containerDiv).html(template);
                    SF.triggerNewContent($('#' + viewOptions.containerDiv));
                }
                else {
                    new SF.ViewNavigator(viewOptions).viewEmbedded();
                    SF.triggerNewContent($("#" + this.options.detailDiv));
                }
                this.onCreated(viewOptions.type);
            },

            viewOptionsForCreating: function (_viewOptions) {
                return $.extend({
                    containerDiv: this.options.detailDiv,
                    prefix: this.options.prefix,
                    requestExtraJsonData: this.extraJsonParams()
                }, _viewOptions);
            },

            newEntity: function (runtimeType) {
                this.runtimeInfo().setEntity(runtimeType, '');
            },

            onCreated: function (runtimeType) {
                this.newEntity(runtimeType);
                this.fireOnEntityChanged(true);
            },

            find: function (_findOptions, _viewOptions) {
                var _self = this;
                var type = this.getRuntimeType(function (type) {
                    _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
                });
            },

            typedFind: function (_findOptions, _viewOptions) {
                if (SF.isEmpty(_findOptions.webQueryName)) {
                    throw "FindOptions webQueryName parameter must not be null in entityLineDetail typedFind. Call find instead";
                }
                var findOptions = this.createFindOptions(_findOptions, _viewOptions);
                SF.FindNavigator.openFinder(findOptions);
            },

            createFindOptions: function (_findOptions, _viewOptions) {
                var self = this;
                return $.extend({
                    prefix: this.options.prefix,
                    onOk: function (selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
                    onOkClosed: function () { self.fireOnEntityChanged(true); },
                    allowMultiple: false
                }, _findOptions);
            },

            onFindingOk: function (selectedItems, _viewOptions) {
                if (selectedItems == null || selectedItems.length != 1) {
                    throw "No item or more than one item was returned from Find Window";
                }
                this.runtimeInfo().setEntity(selectedItems[0].type, selectedItems[0].id);

                //View result in the detailDiv
                var viewOptions = this.viewOptionsForCreating($.extend(_viewOptions, { type: selectedItems[0].type, id: selectedItems[0].id }));
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($("#" + this.options.detailDiv));

                return true;
            },

            removeSpecific: function () {
                $("#" + this.options.detailDiv).html("");
            }
        });

        $.widget("SF.entityList", $.SF.baseLine, {

            options: {}, //baseLine options + reorder

            keys: {
                entity: "sfEntity",
                indexes: "sfIndexes",
                list: "sfList"
            },

            itemSuffix: function () {
                return SF.Keys.toStr;
            },

            updateLinks: function (newToStr, newLink, itemPrefix) {
                $('#' + SF.compose(itemPrefix, SF.Keys.toStr)).html(newToStr);
            },

            extraJsonParams: function (itemPrefix) {
                var extraParams = new Object();

                //If Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
                var staticInfo = this.staticInfo();
                if (staticInfo.isEmbedded()) {
                    var pathInfo = SF.fullPathNodesSelector(itemPrefix);
                    for (var i = 0, l = pathInfo.length; i < l; i++) {
                        var node = pathInfo[i];
                        extraParams[node.id] = node.value;
                    }
                }

                if (staticInfo.isReadOnly()) {
                    extraParams.readOnly = true;
                }

                return extraParams;
            },

            itemRuntimeInfo: function (itemPrefix) {
                return new SF.RuntimeInfo(itemPrefix);
            },

            selectedItemPrefix: function () {
                var $items = this.getItems();
                if ($items.length == 0) {
                    return null;
                }
                var selected = $items.filter(":selected");
                if (selected.length == 0) {
                    return null;
                }
                var nameSelected = selected[0].id;
                return nameSelected.substr(0, nameSelected.indexOf(SF.Keys.toStr) - 1);
            },

            getItems: function () {
                return $(this.pf(this.keys.list) + " > option");
            },

            getLastPrefixIndex: function () {
                var lastPrefixIndex = -1;
                var $items = this.getItems();
                if ($items.length > 0) {
                    var lastId = $items[$items.length - 1].id;
                    lastPrefixIndex = lastId.substring(this.options.prefix.length + 1, lastId.indexOf(this.itemSuffix()) - 1);
                }
                return parseInt(lastPrefixIndex, 10);
            },

            getNewIndex: function (itemPrefix) {
                return parseInt($("#" + SF.compose(itemPrefix, this.keys.indexes)).val().split(";")[1]);
            },

            setNewIndex: function (itemPrefix, newIndex) {
                var $indexes = $("#" + SF.compose(itemPrefix, this.keys.indexes));
                var indexes = $indexes.val().split(";");
                $indexes.val(indexes[0].toString() + ";" + newIndex.toString());
            },

            getLastNewIndex: function () {
                var lastPrefixIndex = this.getLastPrefixIndex();
                if (lastPrefixIndex == -1) {
                    return -1;
                }
                return this.getNewIndex(SF.compose(this.options.prefix, lastPrefixIndex.toString()));
            },

            checkValidation: function (validatorOptions, itemPrefix) {
                if (typeof validatorOptions == "undefined" || typeof validatorOptions.type == "undefined") {
                    throw "validatorOptions.type must be supplied to checkValidation";
                }

                var info = this.itemRuntimeInfo(itemPrefix);
                $.extend(validatorOptions, {
                    prefix: itemPrefix,
                    id: (info.find().length > 0) ? info.id() : ''
                });

                var validator = new SF.PartialValidator(validatorOptions);
                var validatorResult = validator.validate();
                if (!validatorResult.isValid) {
                    if (!confirm(lang.signum.popupErrors)) {
                        $.extend(validatorResult, { acceptChanges: false });
                        return validatorResult;
                    }
                    else
                        validator.showErrors(validatorResult.modelState, true);
                }
                this.updateLinks(validatorResult.newToStr, validatorResult.newLink, itemPrefix);
                $.extend(validatorResult, { acceptChanges: true });
                return validatorResult;
            },

            getEmbeddedTemplate: function (viewOptions) {
                var template = window[SF.compose(this.options.prefix, "sfTemplate")];
                if (!SF.isEmpty(template)) {
                    template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), viewOptions.prefix);
                }
                return template;
            },

            viewOptionsForCreating: function (_viewOptions) {
                var self = this;
                var newPrefixIndex = this.getLastPrefixIndex() + 1;
                var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
                return $.extend({
                    onOk: function (clonedElements) { return self.onCreatingOk(clonedElements, _viewOptions.validationOptions, _viewOptions.type, itemPrefix); },
                    onOkClosed: function () { self.fireOnEntityChanged(); },
                    onCancelled: null,
                    controllerUrl: null,
                    prefix: itemPrefix,
                    requestExtraJsonData: this.extraJsonParams(itemPrefix)
                }, _viewOptions);
            },

            onCreatingOk: function (clonedElements, validatorOptions, runtimeType, itemPrefix) {
                var valOptions = $.extend(validatorOptions || {}, {
                    type: runtimeType
                });
                var validatorResult = this.checkValidation(valOptions, itemPrefix);
                if (validatorResult.acceptChanges) {
                    this.newListItem(clonedElements, itemPrefix, { type: runtimeType, toStr: validatorResult.newToStr });
                }
                return validatorResult.acceptChanges;
            },

            newListItem: function (clonedElements, itemPrefix, item) {
                var listInfo = this.staticInfo();
                var itemInfoValue = this.itemRuntimeInfo(itemPrefix).createValue(item.type, item.id || '', typeof item.id == "undefined" ? 'n' : 'o');
                listInfo.find().after(SF.hiddenInput(SF.compose(itemPrefix, this.keys.indexes), ";" + (this.getLastNewIndex() + 1).toString()))
                    .after(SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue))
                    .after(SF.hiddenDiv(SF.compose(itemPrefix, this.keys.entity), ""));
                $('#' + SF.compose(itemPrefix, this.keys.entity)).append(clonedElements);

                var select = $(this.pf(this.keys.list));
                if (SF.isEmpty(item.toStr)) {
                    item.toStr = "&nbsp;";
                }
                select.append("\n<option id='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' name='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' value='' class='sf-value-line'>" + item.toStr + "</option>");
                select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
                select.children('option:last').attr('selected', true);
            },

            view: function (_viewOptions) {
                var selectedItemPrefix = this.selectedItemPrefix();
                if (SF.isEmpty(selectedItemPrefix)) {
                    return;
                }
                this.viewInIndex(_viewOptions, selectedItemPrefix);
            },

            viewInIndex: function (_viewOptions, selectedItemPrefix) {
                var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
                if (viewOptions.navigate) {
                    var itemInfo = this.itemRuntimeInfo(selectedItemPrefix);
                    if (!SF.isEmpty(itemInfo.id())) {
                        window.open(_viewOptions.controllerUrl.substring(0, _viewOptions.controllerUrl.lastIndexOf("/") + 1) + itemInfo.runtimeType() + "/" + itemInfo.id(), "_blank");
                    }
                    return;
                }
                new SF.ViewNavigator(viewOptions).viewOk();
            },

            viewOptionsForViewing: function (_viewOptions, itemPrefix) {
                var self = this;
                var info = this.itemRuntimeInfo(itemPrefix);
                return $.extend({
                    containerDiv: SF.compose(itemPrefix, self.keys.entity),
                    onOk: function () { return self.onViewingOk(_viewOptions.validationOptions, itemPrefix); },
                    onOkClosed: function () { self.fireOnEntityChanged(); },
                    onCancelled: null,
                    controllerUrl: null,
                    type: info.runtimeType(),
                    id: info.id(),
                    prefix: itemPrefix,
                    requestExtraJsonData: this.extraJsonParams(itemPrefix)
                }, _viewOptions);
            },

            onViewingOk: function (validatorOptions, itemPrefix) {
                var valOptions = $.extend(validatorOptions || {}, {
                    type: this.itemRuntimeInfo(itemPrefix).runtimeType()
                });
                var validatorResult = this.checkValidation(valOptions, itemPrefix);
                return validatorResult.acceptChanges;
            },

            createFindOptions: function (_findOptions) {
                var newPrefixIndex = this.getLastPrefixIndex() + 1;
                var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
                var self = this;
                return $.extend({
                    prefix: itemPrefix,
                    onOk: function (selectedItems) { return self.onFindingOk(selectedItems); },
                    onOkClosed: function () { self.fireOnEntityChanged(); },
                    allowMultiple: true
                }, _findOptions);
            },

            onFindingOk: function (selectedItems) {
                if (selectedItems == null || selectedItems.length == 0) {
                    throw "No item was returned from Find Window";
                }
                var self = this;
                this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                    self.newListItem('', itemPrefix, item);
                });
                return true;
            },

            foreachNewItem: function (selectedItems, itemAction) {
                var lastPrefixIndex = this.getLastPrefixIndex();
                for (var i = 0, l = selectedItems.length; i < l; i++) {
                    var item = selectedItems[i];
                    lastPrefixIndex++;
                    var itemPrefix = SF.compose(this.options.prefix, lastPrefixIndex.toString());
                    itemAction(item, itemPrefix);
                }
            },

            remove: function () {
                var selectedItemPrefix = this.selectedItemPrefix();
                if (SF.isEmpty(selectedItemPrefix)) {
                    return;
                }
                this.removeInIndex(selectedItemPrefix);
            },

            removeInIndex: function (selectedItemPrefix) {
                $.each([SF.Keys.runtimeInfo, SF.Keys.toStr, this.keys.entity, this.keys.indexes], function (i, key) {
                    $("#" + SF.compose(selectedItemPrefix, key)).remove();
                });
                this.fireOnEntityChanged();
            },

            updateButtonsDisplay: function () {
                var hasElements = this.getItems().length > 0;
                $(this.pf("btnRemove")).toggle(hasElements);
                $(this.pf("btnView")).toggle(hasElements);
                $(this.pf("btnUp")).toggle(hasElements);
                $(this.pf("btnDown")).toggle(hasElements);
            },

            moveUp: function (selectedItemPrefix) {
                if (typeof selectedItemPrefix == "undefined") {
                    selectedItemPrefix = this.selectedItemPrefix();
                }

                var suffix = this.itemSuffix();
                var $item = $("#" + SF.compose(selectedItemPrefix, suffix));
                var $itemPrev = $item.prev();

                if ($itemPrev.length == 0) {
                    return;
                }

                var itemPrevId = $itemPrev[0].id;
                var itemPrevPrefix = itemPrevId.substring(0, itemPrevId.indexOf(suffix) - 1);

                var prevNewIndex = this.getNewIndex(itemPrevPrefix);
                this.setNewIndex(selectedItemPrefix, prevNewIndex);
                this.setNewIndex(itemPrevPrefix, prevNewIndex + 1);

                $item.insertBefore($itemPrev);
            },

            moveDown: function (selectedItemPrefix) {
                if (typeof selectedItemPrefix == "undefined") {
                    selectedItemPrefix = this.selectedItemPrefix();
                }

                var suffix = this.itemSuffix();
                var $item = $("#" + SF.compose(selectedItemPrefix, suffix));
                var $itemNext = $item.next();

                if ($itemNext.length == 0) {
                    return;
                }

                var itemNextId = $itemNext[0].id;
                var itemNextPrefix = itemNextId.substring(0, itemNextId.indexOf(suffix) - 1);

                var nextNewIndex = this.getNewIndex(itemNextPrefix);
                this.setNewIndex(selectedItemPrefix, nextNewIndex);
                this.setNewIndex(itemNextPrefix, nextNewIndex - 1);

                $item.insertAfter($itemNext);
            }
        });

        $.widget("SF.entityListDetail", $.SF.entityList, {

            options: {}, //entityList Options + detailDiv

            typedCreate: function (_viewOptions) {
                if (SF.isEmpty(_viewOptions.type)) {
                    throw "ViewOptions type parameter must not be null in entityListDetail typedCreate. Call create instead";
                }
                this.restoreCurrent();
                var viewOptions = this.viewOptionsForCreating(_viewOptions);
                var template = this.getEmbeddedTemplate(viewOptions);
                if (!SF.isEmpty(template)) {
                    $('#' + viewOptions.containerDiv).html(template);
                    SF.triggerNewContent($('#' + viewOptions.containerDiv));
                }
                else {
                    new SF.ViewNavigator(viewOptions).viewEmbedded();
                    SF.triggerNewContent($('#' + viewOptions.containerDiv));
                }
                this.onItemCreated(viewOptions);
            },

            viewOptionsForCreating: function (_viewOptions) {
                var newPrefixIndex = this.getLastPrefixIndex() + 1;
                var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
                return $.extend({
                    containerDiv: this.options.detailDiv,
                    prefix: itemPrefix,
                    requestExtraJsonData: this.extraJsonParams(itemPrefix)
                }, _viewOptions);
            },

            getVisibleItemPrefix: function () {
                var detail = $('#' + this.options.detailDiv);
                var firstId = detail.find(":input[id^=" + this.options.prefix + "]:first");
                if (firstId.length === 0) {
                    return null;
                }
                var id = firstId[0].id;
                var nextSeparator = id.indexOf("_", this.options.prefix.length + 1);
                return id.substring(0, nextSeparator);
            },

            restoreCurrent: function () {
                var itemPrefix = this.getVisibleItemPrefix();
                if (!SF.isEmpty(itemPrefix)) {
                    $('#' + SF.compose(itemPrefix, this.keys.entity))
                        .html('')
                        .append(SF.cloneContents(this.options.detailDiv));
                }
            },

            onItemCreated: function (viewOptions) {
                if (SF.isEmpty(viewOptions.type)) {
                    throw "ViewOptions type parameter must not be null in entityListDetail onItemCreated. Call create instead";
                }

                var itemPrefix = viewOptions.prefix;
                this.newListItem('', itemPrefix, { type: viewOptions.type });
                this.fireOnEntityChanged();
            },

            view: function (_viewOptions) {
                var selectedItemPrefix = this.selectedItemPrefix();
                if (SF.isEmpty(selectedItemPrefix)) {
                    return;
                }
                this.viewInIndex(_viewOptions, selectedItemPrefix);
            },

            viewInIndex: function (_viewOptions, selectedItemPrefix) {
                this.restoreCurrent();
                if (this.isLoaded(selectedItemPrefix)) {
                    this.cloneAndShow(selectedItemPrefix);
                }
                else {
                    var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
                    new SF.ViewNavigator(viewOptions).viewEmbedded();
                    SF.triggerNewContent($('#' + viewOptions.containerDiv));
                }
            },

            viewOptionsForViewing: function (_viewOptions, itemPrefix) {
                var self = this;
                var info = this.itemRuntimeInfo(itemPrefix);
                return $.extend({
                    containerDiv: this.options.detailDiv,
                    type: info.runtimeType(),
                    id: info.id(),
                    prefix: itemPrefix,
                    requestExtraJsonData: this.extraJsonParams(itemPrefix)
                }, _viewOptions);
            },

            isLoaded: function (selectedItemPrefix) {
                return !SF.isEmpty($('#' + SF.compose(selectedItemPrefix, this.keys.entity)).html());
            },

            cloneAndShow: function (selectedItemPrefix) {
                $('#' + this.options.detailDiv)
                    .html('')
                    .append(SF.cloneContents(SF.compose(selectedItemPrefix, this.keys.entity)));

                $('#' + SF.compose(selectedItemPrefix, this.keys.entity))
                    .html('');
            },

            find: function (_findOptions, _viewOptions) {
                var _self = this;
                var type = this.getRuntimeType(function (type) {
                    _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
                });
            },

            typedFind: function (_findOptions, _viewOptions) {
                if (SF.isEmpty(_findOptions.webQueryName)) {
                    throw "FindOptions webQueryName parameter must not be null in entityListDetail typedFind. Call find instead";
                }

                this.restoreCurrent();
                var findOptions = this.createFindOptions(_findOptions, _viewOptions);
                SF.FindNavigator.openFinder(findOptions);
            },

            createFindOptions: function (_findOptions, _viewOptions) {
                var newPrefixIndex = this.getLastPrefixIndex() + 1;
                var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
                var self = this;
                return $.extend({
                    prefix: itemPrefix,
                    onOk: function (selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
                    onOkClosed: function () { self.fireOnEntityChanged(); },
                    allowMultiple: true
                }, _findOptions);
            },

            onFindingOk: function (selectedItems, _viewOptions) {
                if (selectedItems == null || selectedItems.length == 0) {
                    throw "No item was returned from Find Window";
                }
                var self = this;
                this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                    self.newListItem('', itemPrefix, item);
                });
                //View result in the detailDiv
                $(this.pf(this.keys.list)).dblclick();
                return true;
            },

            remove: function () {
                var selectedItemPrefix = this.selectedItemPrefix();
                if (SF.isEmpty(selectedItemPrefix)) {
                    return;
                }
                this.edlineRemoveInIndex(selectedItemPrefix);
            },

            edlineRemoveInIndex: function (itemPrefix) {
                var currentVisible = this.getVisibleItemPrefix();
                if (!SF.isEmpty(currentVisible) && currentVisible == itemPrefix)
                    $('#' + this.options.detailDiv).html('');
                this.removeInIndex(itemPrefix);
            }
        });

        $.widget("SF.entityRepeater", $.SF.entityList, {

            options: {}, //entityList Options + maxElements + removeItemLinkText

            keys: {
                entity: "sfEntity",
                indexes: "sfIndexes",
                itemsContainer: "sfItemsContainer",
                repeaterItem: "sfRepeaterItem",
                repeaterItemClass: "sf-repeater-element"
            },

            itemSuffix: function () {
                return this.keys.repeaterItem;
            },

            getItems: function () {
                return $(this.pf(this.keys.itemsContainer) + " > ." + this.keys.repeaterItemClass);
            },

            canAddItems: function () {
                if (!SF.isEmpty(this.options.maxElements)) {
                    if (this.getItems().length >= +this.options.maxElements) {
                        return false;
                    }
                }
                return true;
            },

            typedCreate: function (_viewOptions) {
                if (SF.isEmpty(_viewOptions.type)) {
                    throw "ViewOptions type parameter must not be null in entityRepeater typedCreate. Call create instead";
                }
                if (!this.canAddItems()) {
                    return;
                }

                var viewOptions = this.viewOptionsForCreating(_viewOptions);
                var template = this.getEmbeddedTemplate(viewOptions);
                if (!SF.isEmpty(template)) { //Template pre-loaded (Embedded Entity): It will be created with "_0" itemprefix => replace it with the current one
                    template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), viewOptions.prefix);
                    this.onItemCreated(template, viewOptions);
                }
                else {
                    var self = this;
                    new SF.ViewNavigator(viewOptions).createEmbedded(function (newHtml) {
                        self.onItemCreated(newHtml, viewOptions);
                    });
                }
            },

            viewOptionsForCreating: function (_viewOptions) {
                var newPrefixIndex = this.getLastPrefixIndex() + 1;
                var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
                return $.extend({
                    containerDiv: "",
                    prefix: itemPrefix,
                    requestExtraJsonData: this.extraJsonParams(itemPrefix)
                }, _viewOptions);
            },

            onItemCreated: function (newHtml, viewOptions) {
                if (SF.isEmpty(viewOptions.type)) {
                    throw "ViewOptions type parameter must not be null in entityRepeater onItemCreated";
                }

                var itemPrefix = viewOptions.prefix;
                this.newRepItem(newHtml, itemPrefix, { type: viewOptions.type });
                this.fireOnEntityChanged();
            },

            newRepItem: function (newHtml, itemPrefix, item) {
                var listInfo = this.staticInfo();
                var itemInfoValue = this.itemRuntimeInfo(itemPrefix).createValue(item.type, item.id || '', typeof item.id == "undefined" ? 'n' : 'o');
                var $div = $("<fieldset id='" + SF.compose(itemPrefix, this.keys.repeaterItem) + "' name='" + SF.compose(itemPrefix, this.keys.repeaterItem) + "' class='" + this.keys.repeaterItemClass + "'>" +
                    "<legend>" +
                    "<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + this.options.removeItemLinkText + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + this.options.removeItemLinkText + "</a>" +
                    (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.entityRepeater_moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-n' data-text='false'>" + lang.signum.entityRepeater_moveUp + "</span>") : "") +
                    (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.entityRepeater_moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-s' data-text='false'>" + lang.signum.entityRepeater_moveDown + "</span>") : "") +
                    "</legend>" +
                    SF.hiddenInput(SF.compose(itemPrefix, this.keys.indexes), ";" + (this.getLastNewIndex() + 1).toString()) +
                    SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue) +
                    "<div id='" + SF.compose(itemPrefix, this.keys.entity) + "' name='" + SF.compose(itemPrefix, this.keys.entity) + "' class='sf-line-entity'>" +
                    "</div>" + //sfEntity
                    "</fieldset>"
                    );

                $(this.pf(this.keys.itemsContainer)).append($div);
                $("#" + SF.compose(itemPrefix, this.keys.entity)).html(newHtml);
                SF.triggerNewContent($("#" + SF.compose(itemPrefix, this.keys.repeaterItem)));
            },

            _getRepeaterCall: function () {
                return "$('#" + this.options.prefix + "').data('entityRepeater')";
            },

            _getRemoving: function (itemPrefix) {
                return this._getRepeaterCall() + ".remove('" + itemPrefix + "');";
            },

            _getMovingUp: function (itemPrefix) {
                return this._getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
            },

            _getMovingDown: function (itemPrefix) {
                return this._getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
            },

            viewOptionsForViewing: function (_viewOptions, itemPrefix) { //Used in onFindingOk
                return $.extend({
                    containerDiv: SF.compose(itemPrefix, this.keys.entity),
                    prefix: itemPrefix,
                    requestExtraJsonData: this.extraJsonParams(itemPrefix)
                }, _viewOptions);
            },

            find: function (_findOptions, _viewOptions) {
                var _self = this;
                var type = this.getRuntimeType(function (type) {
                    _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
                });
            },

            typedFind: function (_findOptions, _viewOptions) {
                if (SF.isEmpty(_findOptions.webQueryName)) {
                    throw "FindOptions webQueryName parameter must not be null in ERep typedFind. Call find instead";
                }
                if (!this.canAddItems()) {
                    return;
                }

                var findOptions = this.createFindOptions(_findOptions, _viewOptions);
                SF.FindNavigator.openFinder(findOptions);
            },

            createFindOptions: function (_findOptions, _viewOptions) {
                var newPrefixIndex = this.getLastPrefixIndex() + 1;
                var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
                var self = this;
                return $.extend({
                    prefix: itemPrefix,
                    onOk: function (selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
                    onOkClosed: function () { self.fireOnEntityChanged(); },
                    allowMultiple: true
                }, _findOptions);
            },

            onFindingOk: function (selectedItems, _viewOptions) {
                if (selectedItems == null || selectedItems.length == 0) {
                    throw "No item was returned from Find Window";
                }
                var self = this;
                this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                    if (!self.canAddItems()) {
                        return;
                    }

                    self.newRepItem('', itemPrefix, item);

                    //View results in the repeater
                    var viewOptions = self.viewOptionsForViewing($.extend(_viewOptions, { type: item.type, id: item.id }), itemPrefix);
                    new SF.ViewNavigator(viewOptions).viewEmbedded();
                    SF.triggerNewContent($(SF.compose(itemPrefix, self.keys.entity)));
                });
                return true;
            },

            remove: function (itemPrefix) {
                $('#' + SF.compose(itemPrefix, this.keys.repeaterItem)).remove();
                this.fireOnEntityChanged();
            },

            updateButtonsDisplay: function () {
                var $buttons = $(this.pf("btnFind"), this.pf("btnFind"));
                if (this.canAddItems()) {
                    $buttons.show();
                }
                else {
                    $buttons.hide();
                }
            }
        });

    })(jQuery);

    SF.fullPathNodesSelector = function (prefix) {
        var pathPrefixes = SF.getPathPrefixes(prefix);
        var nodes = $("#" + SF.Keys.runtimeInfo);
        for (var i = 0, l = pathPrefixes.length; i < l; i++) {
            var current = pathPrefixes[i];
            if (!SF.isEmpty(current)) {
                nodes = nodes.add(SF.getInfoParams(current));
            }
        }
        return nodes;
    };

    SF.getInfoParams = function (prefix) {
        return $("#" + SF.compose(prefix, SF.Keys.runtimeInfo) + ", #" + SF.compose(prefix, $.SF.entityList.prototype.keys.indexes));
    };
});

    
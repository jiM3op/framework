﻿import * as React from 'react'
import { TabPane, TabContent } from 'reactstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../../../../Framework/Signum.React/Scripts/Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import Typeahead from '../../../../Framework/Signum.React/Scripts/Lines/Typeahead'
import { EntityListBase, EntityListBaseProps } from '../../../../Framework/Signum.React/Scripts/Lines/EntityListBase'
import { RenderEntity } from '../../../../Framework/Signum.React/Scripts/Lines/RenderEntity'

interface IGridEntity {
    row: number;
    startColumn: number;
    columns: number
}

export interface EntityGridRepeaterProps extends EntityListBaseProps {
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    createAsLink?: boolean;
    move?: boolean;
    resize?: boolean;
}


export interface EntityGridRepaterState extends EntityGridRepeaterProps {
    dragMode?: string;
    initialPageX?: number;
    originalStartColumn?: number;
    currentItem?: TypeContext<ModifiableEntity & IGridEntity>;
    currentRow?: number;
}

export class EntityGridRepeater extends EntityListBase<EntityGridRepeaterProps, EntityGridRepaterState> {

    calculateDefaultState(state: EntityGridRepeaterProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
        state.move = true;
        state.resize = true;
        state.remove = true;
    }

    renderInternal() {
        const s = this.state;
        return (
            <fieldset className={classes("SF-grid-repeater-field SF-control-container", this.state.ctx.errorClass) }>
                <legend>
                    <div>
                        <span>{this.state.labelText}</span>
                        <span className="pull-right">
                            {this.renderCreateButton(false) }
                            {this.renderFindButton(false) }
                        </span>
                    </div>
                </legend>
                <div className="row rule">
                    { Array.range(0, 12).map(i =>
                        <div className="col-sm-1" key={i}>
                            <div className="ruleItem"/>
                        </div>
                    ) }
                </div>
                <div className={s.dragMode == "move" ? "sf-dragging" : undefined} onDrop={this.handleOnDrop}>
                    {
                        mlistItemContext(this.state.ctx)
                            .map((mlec, i) => ({
                                ctx: mlec as TypeContext<ModifiableEntity & IGridEntity>,
                                index: i
                            }))
                            .groupBy(p => p.ctx.value.row.toString())
                            .orderBy(gr => parseInt(gr.key))
                            .flatMap((gr, i, groups) => [
                                this.renderSeparator(parseInt(gr.key)),
                                <div className="row items-row" key={"row" + gr.key} onDragOver={e => this.handleItemsRowDragOver(e, parseInt(gr.key)) }>
                                    {gr.elements.orderBy(a => a.ctx.value.startColumn).map((p, j, list) => {
                                        let item = this.props.getComponent!(p.ctx);
                                        const s = this.state;
                                        item = React.cloneElement(item, {
                                            onResizerDragStart: p.ctx.readOnly || !s.resize ? undefined : (resizer, e) => this.handleResizeDragStart(resizer, e, p.ctx),
                                            onTitleDragStart: p.ctx.readOnly || !s.move ? undefined : (e) => this.handleMoveDragStart(e, p.ctx),
                                            onRemove: p.ctx.readOnly || !s.remove ? undefined : (e) => this.handleRemoveElementClick(e, p.index),
                                        } as EntityGridItemProps);

                                        const last = j == 0 ? undefined : list[j - 1].ctx.value;

                                        const offset = p.ctx.value.startColumn - (last ? (last.startColumn + last.columns) : 0);

                                        return (
                                            <div key={j} className={`sf-grid-element col-sm-${p.ctx.value.columns} col-sm-offset-${offset}`}>
                                                {item}
                                                {/*StartColumn: {p.ctx.value.startColumn} | Columns: {p.ctx.value.columns} | Row: {p.ctx.value.row}*/}
                                            </div>
                                        );
                                    }) }
                                </div>,
                                i == groups.length - 1 && this.renderSeparator(parseInt(gr.key) + 1)
                            ])

                    }
                </div>
            </fieldset>
        );
    }

    renderSeparator(rowIndex: number) {
        return (
            <div className={classes("row separator-row", this.state.currentRow == rowIndex ? "sf-over" : undefined) } key={"sep" + rowIndex}
                onDragOver = {e => this.handleRowDragOver(e, rowIndex) }
                onDragEnter = {e => this.handleRowDragOver(e, rowIndex) }
                onDragLeave = {() => this.handleRowDragLeave() }
                onDrop = {e => this.handleRowDrop(e, rowIndex) } />
        );
    }


    handleRowDragOver = (e: React.DragEvent<any>, row: number) => {
        e.dataTransfer.dropEffect = "move";
        e.preventDefault();
        if (this.state.currentRow != row) {
            this.state.currentRow = row;
            this.forceUpdate();
        }
    }

    handleRowDragLeave = () => {
        this.state.currentRow = undefined;
        this.forceUpdate();
    }

    handleRowDrop = (e: React.DragEvent<any>, row: number) => {

        const list = this.state.ctx.value!.map(a => a.element as ModifiableEntity & IGridEntity);

        const c = this.state.currentItem!.value;

        list.filter(a => a != c && a.row >= row).forEach(a => { a.row++; a.modified = true; });
        c.row = row;
        c.startColumn = 0;
        c.columns = 12;
        c.modified = true;

        const s = this.state;
        s.dragMode = undefined;
        s.initialPageX = undefined;
        s.originalStartColumn = undefined;
        s.currentItem = undefined;
        s.currentRow = undefined;
        this.forceUpdate();
    }


    handleCreateClick = (event: React.SyntheticEvent<any>) => {

        event.preventDefault();

        const promise = this.props.onCreate ?
            this.props.onCreate() : this.defaultCreate();

        if (!promise)
            return;

        promise
            .then((e: ModifiableEntity & IGridEntity) => {

                if (!e)
                    return;

                if (!e.Type)
                    throw new Error("Should be an entity");

                const list = this.props.ctx.value!;
                if (e.row == undefined)
                    e.row = list.length == 0 ? 0 : list.map(a => (a.element as any as IGridEntity).row).max() + 1;
                if (e.startColumn == undefined)
                    e.startColumn = 0;
                if (e.columns == undefined)
                    e.columns = 12;

                list.push({ rowId: null, element: e });
                this.setValue(list);
            }).done();
    };

    handleOnDrop = (event: React.SyntheticEvent<any>) => {


        const s = this.state;
        s.dragMode = undefined;
        s.initialPageX = undefined;
        s.originalStartColumn = undefined;
        s.currentItem = undefined;
        s.currentRow = undefined;
        this.forceUpdate();
    }



    handleResizeDragStart = (resizer: "left" | "right", e: React.DragEvent<any>, mlec: TypeContext<ModifiableEntity & IGridEntity>) => {
        e.dataTransfer.effectAllowed = "move";
        const de = e.nativeEvent as DragEvent;

        const s = this.state;
        s.dragMode = resizer;
        s.initialPageX = undefined;
        s.originalStartColumn = undefined;
        s.currentItem = mlec;
        s.currentRow = undefined;
        this.forceUpdate();
    }

    handleMoveDragStart = (e: React.DragEvent<any>, mlec: TypeContext<ModifiableEntity & IGridEntity>) => {
        e.dataTransfer.effectAllowed = "move";
        const de = e.nativeEvent as DragEvent;

        const s = this.state;
        s.dragMode = "move";
        s.initialPageX = de.pageX;
        s.originalStartColumn = mlec.value.startColumn;
        s.currentItem = mlec;
        s.currentRow = undefined;
        this.forceUpdate();
    }

    handleItemsRowDragOver = (e: React.DragEvent<any>, row: number) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = "move";
        const de = e.nativeEvent as DragEvent;
        const s = this.state;
        const list = s.ctx.value!.map(a => a.element as ModifiableEntity & IGridEntity);
        const c = s.currentItem!.value;
        const rect = (e.currentTarget as HTMLDivElement).getBoundingClientRect();



        if (s.dragMode == "move") {
            const offset = de.pageX - s.initialPageX!;
            const dCol = Math.round((offset / rect.width) * 12);
            let newCol = s.originalStartColumn! + dCol;
            let start = list.filter(a => a != c && a.row == row && a.startColumn <= newCol).map(a => a.startColumn + a.columns).max();
            if (!isFinite(start))
                start = 0;

            let end = list.filter(a => a != c && a.row == row && a.startColumn > newCol).map(a => a.startColumn - c.columns).min();
            if (!isFinite(end))
                end = 12 - c.columns;

            if (start > end) {
                e.dataTransfer.dropEffect = "none";
                return; //Doesn't fit
            }

            newCol = Math.max(start, Math.min(newCol, end));

            if (newCol != c.startColumn || c.row != row) {
                c.startColumn = newCol;
                c.row = row;
                c.modified = true;
                this.forceUpdate();
            }
        } else {
            const offsetX = (de.pageX + (s.dragMode == "right" ? 15 : -15)) - rect.left;
            let col = Math.round((offsetX / rect.width) * 12);

            if (s.dragMode == "left") {
                const max = list.filter(a => a != c && a.row == c.row && a.startColumn < c.startColumn).map(a => a.startColumn + a.columns).max();
                col = Math.max(col, max);

                const cx = c.startColumn - col;
                if (cx != 0) {
                    c.startColumn = col;
                    c.columns += cx;
                    c.modified = true;

                    this.forceUpdate();
                }
            }
            else if (s.dragMode == "right") {
                const min = list.filter(a => a != c && a.row == c.row && a.startColumn > c.startColumn).map(a => a.startColumn).min();
                col = Math.min(col, min);
                if (col != c.startColumn + c.columns) {
                    c.columns = col - c.startColumn;
                    c.modified = true;

                    this.forceUpdate();
                }
            }
        }
    }


}



export interface EntityGridItemProps {
    title?: React.ReactElement<any>;
    bsStyle?: string;
    children?: React.ReactNode;

    onResizerDragStart?: (resizer: "left" | "right", e: React.DragEvent<any>) => void;
    onTitleDragStart?: (e: React.DragEvent<any>) => void;
    onRemove?: (e: React.MouseEvent<any>) => void;
}


export class EntityGridItem extends React.Component<EntityGridItemProps>{

    render() {

        return (
            <div className={"panel panel-" + (this.props.bsStyle ? this.props.bsStyle.toLowerCase() : "default") }>
                <div className="panel-heading" draggable={!!this.props.onTitleDragStart}
                    onDragStart={this.props.onTitleDragStart} >
                    {this.props.onRemove &&
                        <a className="sf-line-button sf-remove pull-right" onClick={this.props.onRemove}
                            title={EntityControlMessage.Remove.niceToString() }>
                            <span className="glyphicon glyphicon-remove"></span>
                        </a>
                    }
                    {this.props.title}
                </div>
                <div className="panel-body">
                    { this.props.children  }
                </div>
                {this.props.onResizerDragStart &&
                    <div className="sf-leftHandle" draggable={true}
                        onDragStart={e => this.props.onResizerDragStart!("left", e) }>
                    </div>
                }
                {this.props.onResizerDragStart &&
                    <div className="sf-rightHandle" draggable={true}
                        onDragStart={e => this.props.onResizerDragStart!("right", e) }>
                    </div>
                }
            </div>
        );

    }
}



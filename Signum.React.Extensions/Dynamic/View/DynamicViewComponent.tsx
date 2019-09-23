import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ValueLine, EntityTable } from '@framework/Lines'
import { ModifiableEntity, JavascriptMessage, NormalWindowMessage } from '@framework/Signum.Entities'
import { classes } from '@framework/Globals'
import { getTypeInfo } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import MessageModal from '@framework/Modals/MessageModal'
import { TypeContext } from '@framework/TypeContext'
import * as Operations from '@framework/Operations'
import * as EntityOperations from '@framework/Operations/EntityOperations'
import { BaseNode } from './Nodes'
import { DesignerContext, DesignerNode, RenderWithViewOverrides } from './NodeUtils'
import * as DynamicViewClient from '../DynamicViewClient'
import { DynamicViewTabs } from './DynamicViewTabs'
import { DynamicViewInspector, CollapsableTypeHelp } from './Designer'
import ShowCodeModal from './ShowCodeModal'
import { DynamicViewEntity, DynamicViewOperation, DynamicViewMessage, DynamicViewPropEmbedded } from '../Signum.Entities.Dynamic'
import { Dropdown, DropdownButton, Tabs, Tab } from 'react-bootstrap';
import "./DynamicView.css"
import { AutoFocus } from '@framework/Components/AutoFocus';

export interface DynamicViewComponentProps {
  ctx: TypeContext<ModifiableEntity>;
  initialDynamicView: DynamicViewEntity;
  //...extraProps
}

export interface DynamicViewComponentState {
  isDesignerOpen: boolean;
  rootNode: BaseNode;
  selectedNode: DesignerNode<BaseNode>;
  dynamicView: DynamicViewEntity;
  viewOverrides?: Navigator.ViewOverride<ModifiableEntity>[];
}

export default class DynamicViewComponent extends React.Component<DynamicViewComponentProps, DynamicViewComponentState>{

  constructor(props: DynamicViewComponentProps) {
    super(props);

    const rootNode = JSON.parse(props.initialDynamicView.viewContent!) as BaseNode;
    this.state = {
      dynamicView: props.initialDynamicView,
      isDesignerOpen: false,
      rootNode: rootNode,
      selectedNode: this.getZeroNode().createChild(rootNode)
    };
  }

  componentWillMount() {
    Navigator.viewDispatcher.getViewOverrides(this.props.ctx.value.Type)
      .then(vos => this.setState({ viewOverrides: vos }))
      .done();
  }

  getZeroNode() {

    var { ctx, children, initialDynamicView, ...extraProps } = this.props;

    var context: DesignerContext = {
      onClose: this.handleClose,
      refreshView: () => { this.setState({ selectedNode: this.state.selectedNode.reCreateNode() }); },
      getSelectedNode: () => this.state.isDesignerOpen ? this.state.selectedNode : undefined,
      setSelectedNode: (newNode) => this.setState({ selectedNode: newNode }),
      props: extraProps,
      propTypes: initialDynamicView.props.toObject(mle => mle.element.name, mle => mle.element.type),
      locals: {},
      localsCode: initialDynamicView.locals,
    };

    return DesignerNode.zero(context, ctx.value.Type);
  }

  handleReload = (dynamicView: DynamicViewEntity) => {

    this.setState({
      dynamicView: dynamicView,
      rootNode: JSON.parse(dynamicView.viewContent!) as BaseNode,
      selectedNode: this.getZeroNode().createChild(this.state.rootNode)
    });
  }

  handleOpen = () => {
    this.setState({ isDesignerOpen: true });
  }

  handleClose = () => {
    this.setState({ isDesignerOpen: false });
  }

  render() {


    const rootNode = this.getZeroNode().createChild(this.state.rootNode);
    const ctx = this.props.ctx;

    if (this.state.viewOverrides == null)
      return null;

    var topMostEntity = ctx.frame && ctx.frame.pack && ctx.frame.pack.entity;
    

    var vos = this.state.viewOverrides.filter(a => a.viewName == this.state.dynamicView.viewName);

    if (!Navigator.isViewable(DynamicViewEntity)) {
      return (
        <div className="design-content">
          <RenderWithViewOverrides dn={rootNode} parentCtx={ctx} vos={vos} />
        </div>
      );
    }
    return (<div className="design-main">
      <div className={classes("design-left", this.state.isDesignerOpen && "open")}>
        {!this.state.isDesignerOpen ?
          <span onClick={this.handleOpen}><FontAwesomeIcon icon={["fas", "edit"]} className="design-open-icon" /></span> :
          <DynamicViewDesigner
            rootNode={rootNode}
            dynamicView={this.state.dynamicView}
            onReload={this.handleReload}
            onLoseChanges={this.handleLoseChanges}
            typeName={ctx.value.Type} />
        }
      </div>
      <div className={classes("design-content", this.state.isDesignerOpen && "open")}>
        <AutoFocus disabled={topMostEntity != ctx.value}>
          <RenderWithViewOverrides dn={rootNode} parentCtx={ctx} vos={vos} />
        </AutoFocus>
      </div>
    </div>);
  }

  handleLoseChanges = () => {
    const node = JSON.stringify(this.state.rootNode);

    if (this.state.dynamicView.isNew || node != this.state.dynamicView.viewContent) {
      return MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.loseCurrentChanges.niceToString(),
        buttons: "yes_no",
        style: "warning",
        icon: "warning"
      }).then(result => { return result == "yes"; });
    }

    return Promise.resolve(true);
  }
}

interface DynamicViewDesignerProps {
  rootNode: DesignerNode<BaseNode>;
  dynamicView: DynamicViewEntity;
  onLoseChanges: () => Promise<boolean>;
  onReload: (dynamicView: DynamicViewEntity) => void;
  typeName: string;
}

interface DynamicViewDesignerState {
  viewNames?: string[];
  isDropdownOpen: boolean;
}

class DynamicViewDesigner extends React.Component<DynamicViewDesignerProps, DynamicViewDesignerState>{

  constructor(props: DynamicViewDesignerProps) {
    super(props);
    this.state = { isDropdownOpen: false };
  }

  render() {
    var dv = this.props.dynamicView;
    var ctx = TypeContext.root(dv);

    return (
      <div className="code-container">
        <button type="button" className="close" aria-label="Close" style={{ float: "right" }} onClick={this.props.rootNode.context.onClose}><span aria-hidden="true">×</span></button>
        <h3>
          <small>{Navigator.getTypeTitle(this.props.dynamicView, undefined)}</small>
        </h3>
        <ValueLine ctx={ctx.subCtx(e => e.viewName)} formGroupStyle="SrOnly" placeholderLabels={true} />
        {this.renderButtonBar()}
        <DynamicViewTabs ctx={ctx} rootNode={this.props.rootNode}/>
        <CollapsableTypeHelp initialTypeName={dv.entityType!.cleanName} />
      </div>
    );
  }



  reload(entity: DynamicViewEntity) {
    this.setState({ viewNames: undefined });
    this.props.onReload(entity);
  }

  handleSave = () => {

    this.props.dynamicView.viewContent = JSON.stringify(this.props.rootNode.node);
    this.props.dynamicView.modified = true;

    Operations.API.executeEntity(this.props.dynamicView, DynamicViewOperation.Save)
      .then(pack => {
        this.reload(pack.entity);
        DynamicViewClient.cleanCaches();
        return EntityOperations.notifySuccess();
      })
      .done();
  }

  handleCreate = () => {

    this.props.onLoseChanges().then(goahead => {
      if (!goahead)
        return;

      DynamicViewClient.createDefaultDynamicView(this.props.typeName)
        .then(entity => { this.reload(entity); return EntityOperations.notifySuccess(); })
        .done();

    }).done();
  }

  handleClone = () => {

    this.props.onLoseChanges().then(goahead => {
      if (!goahead)
        return;

      Operations.API.constructFromEntity(this.props.dynamicView, DynamicViewOperation.Clone)
        .then(pack => { this.reload(pack.entity); return EntityOperations.notifySuccess(); })
        .done();
    }).done();
  }

  handleChangeView = (viewName: string) => {
    this.props.onLoseChanges().then(goahead => {
      if (!goahead)
        return;

      DynamicViewClient.API.getDynamicView(this.props.typeName, viewName)
        .then(entity => { this.reload(entity!); })
        .done();
    }).done();
  }

  handleOnToggle = () => {
    if (!this.state.isDropdownOpen && !this.state.viewNames)
      DynamicViewClient.API.getDynamicViewNames(this.props.typeName)
        .then(viewNames => this.setState({ viewNames: viewNames }))
        .done();

    this.setState({ isDropdownOpen: !this.state.isDropdownOpen });
  }

  handleShowCode = () => {

    ShowCodeModal.showCode(this.props.typeName, this.props.rootNode.node);
  }

  renderButtonBar() {

    var operations = Operations.operationInfos(getTypeInfo(DynamicViewEntity)).toObject(a => a.key);

    return (
      <div className="btn-group btn-group-sm" role="group" style={{ marginBottom: "5px" }}>
        {operations[DynamicViewOperation.Save.key] && <button type="button" className="btn btn-primary" onClick={this.handleSave}>{operations[DynamicViewOperation.Save.key].niceName}</button>}
        <button type="button" className="btn btn-success" onClick={this.handleShowCode}>Show code</button>
        <Dropdown onToggle={this.handleOnToggle} show={this.state.isDropdownOpen} >
          <Dropdown.Toggle id="bg-nested-dropdown" size="sm">
            {" … "}
          </Dropdown.Toggle>
          <Dropdown.Menu>
            {operations[DynamicViewOperation.Create.key] && <Dropdown.Item onClick={this.handleCreate}>{operations[DynamicViewOperation.Create.key].niceName}</Dropdown.Item>}
            {operations[DynamicViewOperation.Clone.key] && !this.props.dynamicView.isNew && <Dropdown.Item onClick={this.handleClone}>{operations[DynamicViewOperation.Clone.key].niceName}</Dropdown.Item>}
            {this.state.viewNames && this.state.viewNames.length > 0 && <Dropdown.Divider />}
            {this.state.viewNames && this.state.viewNames.map(vn => <Dropdown.Item key={vn}
              className={classes("sf-dynamic-view", vn == this.props.dynamicView.viewName && "active")}
              onClick={() => this.handleChangeView(vn)}>
              {vn}
            </Dropdown.Item>)}
          </Dropdown.Menu>
        </Dropdown>
      </div >
    );
  }
}


//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export module DiffLogMessage {
  export const PreviousLog = new MessageKey("DiffLogMessage", "PreviousLog");
  export const NextLog = new MessageKey("DiffLogMessage", "NextLog");
  export const CurrentEntity = new MessageKey("DiffLogMessage", "CurrentEntity");
  export const NavigatesToThePreviousOperationLog = new MessageKey("DiffLogMessage", "NavigatesToThePreviousOperationLog");
  export const DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState");
  export const StateWhenTheOperationStarted = new MessageKey("DiffLogMessage", "StateWhenTheOperationStarted");
  export const DifferenceBetweenInitialStateAndFinalState = new MessageKey("DiffLogMessage", "DifferenceBetweenInitialStateAndFinalState");
  export const StateWhenTheOperationFinished = new MessageKey("DiffLogMessage", "StateWhenTheOperationFinished");
  export const DifferenceBetweenFinalStateAndTheInitialStateOfNextLog = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateAndTheInitialStateOfNextLog");
  export const NavigatesToTheNextOperationLog = new MessageKey("DiffLogMessage", "NavigatesToTheNextOperationLog");
  export const DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity");
  export const NavigatesToTheCurrentEntity = new MessageKey("DiffLogMessage", "NavigatesToTheCurrentEntity");
}

export const DiffLogMixin = new Type<DiffLogMixin>("DiffLogMixin");
export interface DiffLogMixin extends Entities.MixinEntity {
  Type: "DiffLogMixin";
  initialState: Basics.BigStringEmbedded;
  finalState: Basics.BigStringEmbedded;
  cleaned: boolean;
}

export module TimeMachineMessage {
  export const TimeMachine = new MessageKey("TimeMachineMessage", "TimeMachine");
  export const EntityDeleted = new MessageKey("TimeMachineMessage", "EntityDeleted");
  export const CompareVersions = new MessageKey("TimeMachineMessage", "CompareVersions");
}

export module TimeMachinePermission {
  export const ShowTimeMachine : Authorization.PermissionSymbol = registerSymbol("Permission", "TimeMachinePermission.ShowTimeMachine");
}



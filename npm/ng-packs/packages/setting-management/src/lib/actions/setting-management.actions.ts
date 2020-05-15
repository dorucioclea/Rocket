import { SettingTab } from '@aiwins/ng.theme.shared';

export class SetSelectedSettingTab {
  static readonly type = '[SettingManagement] Set Selected Tab';
  constructor(public payload: SettingTab) {}
}

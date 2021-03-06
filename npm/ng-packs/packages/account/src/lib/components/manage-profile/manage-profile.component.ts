import { fadeIn } from '@aiwins/ng.theme.shared';
import { transition, trigger, useAnimation } from '@angular/animations';
import { Component } from '@angular/core';
import { eAccountComponents } from '../../enums/components';

@Component({
  selector: 'rocket-manage-profile',
  templateUrl: './manage-profile.component.html',
  animations: [trigger('fadeIn', [transition(':enter', useAnimation(fadeIn))])],
})
export class ManageProfileComponent {
  selectedTab = 0;

  changePasswordKey = eAccountComponents.ChangePassword;

  personalSettingsKey = eAccountComponents.PersonalSettings;
}

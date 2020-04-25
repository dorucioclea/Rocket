import { CoreModule } from '@rocket/ng.core';
import { NgModule, Provider } from '@angular/core';
import { NgxsModule } from '@ngxs/store';
import { RolesComponent } from './components/roles/roles.component';
import { IdentityRoutingModule } from './identity-routing.module';
import { IdentityState } from './states/identity.state';
import { NgbTabsetModule, NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { ThemeSharedModule } from '@rocket/ng.theme.shared';
import { UsersComponent } from './components/users/users.component';
import { PermissionManagementModule } from '@rocket/ng.permission-management';
import { NgxValidateCoreModule } from '@ngx-validate/core';

@NgModule({
  declarations: [RolesComponent, UsersComponent],
  imports: [
    NgxsModule.forFeature([IdentityState]),
    CoreModule,
    IdentityRoutingModule,
    NgbTabsetModule,
    ThemeSharedModule,
    NgbDropdownModule,
    PermissionManagementModule,
    NgxValidateCoreModule,
  ],
})
export class IdentityModule {}

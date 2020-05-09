import { createHttpFactory, HttpMethod, SpectatorHttp } from '@ngneat/spectator/jest';
import { ProfileService, RestService } from '../services';
import { Store } from '@ngxs/store';

describe('ProfileService', () => {
  let spectator: SpectatorHttp<ProfileService>;
  const createHttp = createHttpFactory({
    dataService: ProfileService,
    providers: [RestService],
    mocks: [Store],
  });

  beforeEach(() => (spectator = createHttp()));

  it('should send a GET to my-profile API', () => {
    spectator.get(Store).selectSnapshot.andReturn('https://rocket.io');
    spectator.service.get().subscribe();
    spectator.expectOne('https://rocket.io/api/identity/my-profile', HttpMethod.GET);
  });

  it('should send a POST to change-password API', () => {
    const mock = { currentPassword: 'test', newPassword: 'test' };
    spectator.get(Store).selectSnapshot.andReturn('https://rocket.io');
    spectator.service.changePassword(mock).subscribe();
    const req = spectator.expectOne('https://rocket.io/api/identity/my-profile/change-password', HttpMethod.POST);
    expect(req.request.body).toEqual(mock);
  });

  it('should send a PUT to my-profile API', () => {
    const mock = {
      email: 'info@aiwinssoft.com',
      userName: 'admin',
      name: 'John',
      surname: 'Doe',
      phoneNumber: '+123456',
    };
    spectator.get(Store).selectSnapshot.andReturn('https://rocket.io');
    spectator.service.update(mock).subscribe();
    const req = spectator.expectOne('https://rocket.io/api/identity/my-profile', HttpMethod.PUT);
    expect(req.request.body).toEqual(mock);
  });
});
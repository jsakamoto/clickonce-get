module Publish {
    var app = angular.module('Publish', ['ngResource']);

    export class DetailController {
        private api: ng.resource.IResourceClass<ng.resource.IResource<any>>;

        constructor($resource: ng.resource.IResourceService) {
            this.api = $resource('/api/myapps/:id');
        }

        public remove(appToDelete: any): void {
            if (confirm('Delete "' + (appToDelete.Title || appToDelete.Name) + '" - Are you sure?') == false) return;

            var fromOfDetail = window.sessionStorage.getItem('fromOfDetail');
            var goBackUrl = fromOfDetail == 'myApps' ? '/home/MyApps' : '/';

            this.api.remove({ id: appToDelete.Name })
                .$promise
                .then(() => { window.location.href = goBackUrl; })
                .catch(() => { alert('Oops... something wrong.'); });
        }
    }

    app.controller('DetailController', DetailController);
}
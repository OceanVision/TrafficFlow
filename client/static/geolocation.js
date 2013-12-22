/* ========== G E O L O C A T I O N   C L A S S ========== */
var Geolocation = Class.create({
    isLocationAvailable : false,
    locationPointer : {
        handle : null,
        position : null
    },

    getLocation : function(successCallback, errorCallback) {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function(position) {
                geolocation.isLocationAvailable = true;
                // TODO: odhardkodować zoom
                successCallback(new SphericalCoords(position.coords.longitude, position.coords.latitude, 17),
                    position.coords.accuracy);
            }, function(error) {
                geolocation.isLocationAvailable = false;
                if (typeof errorCallback !== 'undefined') {
                    errorCallback(error);
                }
            });
        } else {
            this.isLocationAvailable = false;
            if (typeof errorCallback !== 'undefined') {
                errorCallback(null);
            }
        };
    }
});
/* ========== G E O L O C A T I O N   C L A S S ========== */
var Geolocation = Class.create({
    isAvailable : false,

    getLocation : function(successCallback, errorCallback) {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function(position) {
                geolocation.isAvailable = true;
                // TODO: odhardkodować zoom
                successCallback(new SphericalCoords(position.coords.longitude, position.coords.latitude, 17),
                    position.coords.accuracy);
            }, function(error) {
                geolocation.isAvailable = false;
                if (typeof errorCallback != 'undefined') {
                    errorCallback(error);
                }
            }, {
                enableHighAccuracy : true
            });
        } else {
            this.isAvailable = false;
            if (typeof errorCallback != 'undefined') {
                errorCallback(null);
            }
        };
    }
});
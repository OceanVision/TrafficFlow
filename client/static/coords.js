/* ========== C O O R D S   I N T E R F A C E ========== */
var Coords = Class.create({
    initialize : function(type, first, second, zoom) {
        this.type = type;
        this.first = first;
        this.second = second;
        this.zoom = zoom;
    },

    getType : function() {
        return this.type;
    },

    compare : function(coords) {
        if (this.first == coords.first && this.second == coords.second && this.zoom == coords.zoom) {
            return true;
        }
        return false;
    }
});

/* ========== O S M   C O O R D S   C L A S S ========== */
var OSMCoords = Class.create(Coords, {
    initialize : function($super, first, second, zoom) {
        $super('osm', first, second, zoom);
    },

    toOSM : function(moveX, moveY) {
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;

        return new OSMCoords(this.first + moveX, this.second + moveY, this.zoom);
    },

    toCartesian : function(moveX, moveY) { // uwaga, to dla zoomu 17!
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;

        return new CartesianCoords(256 * (this.first - 65532) + moveX, 256 * (43598 - this.second) + moveY, this.zoom);
    },

    toSpherical : function(moveX, moveY) {
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;

        var n = Math.PI - 2 * Math.PI * this.second / Math.pow(2, this.zoom),
            longitude = this.first / Math.pow(2, this.zoom) * 360 - 180,
            latitude = 180 / Math.PI * Math.atan(0.5 * (Math.exp(n) - Math.exp(-n)));
        return new SphericalCoords(longitude + moveX, latitude + moveY, this.zoom);
    }
});

/* ========== C A R T E S I A N   C O O R D S   C L A S S ========== */
var CartesianCoords = Class.create(Coords, {
    initialize : function($super, first, second, zoom) {
        $super('cartesian', first, second, zoom);
    },

    toOSM : function(moveX, moveY) { // uwaga, to dla zoomu 17!
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;

        var x = this.first - this.first % 256,
            y = this.second - this.second % 256;
        return new OSMCoords(x / 256 + 65532 + moveX, 43598 - y / 256 + moveY, this.zoom);
    },

    toCartesian : function(moveX, moveY) {
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;

        return new CartesianCoords(this.first + moveX, this.second + moveY, this.zoom);
    },

    toSpherical : function(moveX, moveY) {
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;

        var osm1 = this.toOSM(),
            osm2 = this.toOSM(1, 1),

            spherical1 = osm1.toSpherical(),
            spherical2 = osm2.toSpherical(),

            unitLon = spherical2.first - spherical1.first,
            unitLat = spherical2.second - spherical1.second,

            deltaLon = ((this.first % 256) / 256) * unitLon,
            deltaLat = ((this.second % 256) / 256) * unitLat,

            longitude = spherical1.first + deltaLon,
            latitude = spherical1.second - deltaLat;
        return new SphericalCoords(longitude + moveX, latitude + moveY, this.zoom);
    }
});

/* ========== S P H E R I C A L   C O O R D S   C L A S S ========== */
var SphericalCoords = Class.create(Coords, {
    initialize : function($super, first, second, zoom) {
        $super('spherical', first, second, zoom);
    },

    toOSM : function(moveX, moveY) {
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;

        var x = Math.floor((this.first + 180) / 360 * Math.pow(2, this.zoom)),
            y = Math.floor((1 - Math.log(Math.tan(this.second * Math.PI / 180) + 1
                / Math.cos(this.second * Math.PI / 180)) / Math.PI) / 2 * Math.pow(2, this.zoom));
        return new OSMCoords(x + moveX, y + moveY, this.zoom);
    },

    toCartesian : function(moveX, moveY) { // uwaga, to dla zoomu 17!
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;

        var osm1 = this.toOSM(),
            osm2 = this.toOSM(1, 1),

            spherical1 = osm1.toSpherical(),
            spherical2 = osm2.toSpherical(),

            unitLon = spherical2.first - spherical1.first,
            unitLat = spherical2.second - spherical1.second,

            deltaX = 256 * ((this.first - spherical1.first) / unitLon),
            deltaY = 256 * ((this.second - spherical1.second) / unitLat),

            x = 256 * (osm1.first - 65532) + deltaX,
            y = 256 * (43598 - osm1.second) - deltaY;
        return new CartesianCoords(x + moveX, y + moveY, this.zoom);
    },

    toSpherical : function(moveX, moveY) {
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;

        return new SphericalCoords(this.first + moveX, this.second + moveY, this.zoom);
    }
});
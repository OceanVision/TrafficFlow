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

    toOSM : function() {
        return this;
    },

    toCartesian : function(moveX, moveY) { // uwaga, to dla zoomu 17!
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;
        return new CartesianCoords(256 * (this.first + moveX - 65532), 256 * (43598 - (this.second + moveY)), this.zoom);
    },

    toSpherical : function() {
        var n = Math.PI - 2 * Math.PI * this.second / Math.pow(2, this.zoom),
            longitude = this.first / Math.pow(2, this.zoom) * 360 - 180,
            latitude = 180 / Math.PI * Math.atan(0.5 * (Math.exp(n) - Math.exp(-n)));
        return new SphericalCoords(longitude, latitude, this.zoom);
    }
});

/* ========== C A R T E S I A N   C O O R D S   C L A S S ========== */
var CartesianCoords = Class.create(Coords, {
    initialize : function($super, first, second, zoom) {
        $super('cartesian', first, second, zoom);
    },

    toOSM : function() { // uwaga, to dla zoomu 17!
        return new OSMCoords(this.first / 256 + 65532, 43598 - this.second / 256, this.zoom);
    },

    toCartesian : function() {
        return this;
    },

    toSpherical : function() {
        var osm = this.toOSM();
        var n = Math.PI - 2 * Math.PI * osm.second / Math.pow(2, this.zoom),
            longitude = osm.first / Math.pow(2, this.zoom) * 360 - 180,
            latitude = 180 / Math.PI * Math.atan(0.5 * (Math.exp(n) - Math.exp(-n)));
        return new SphericalCoords(longitude, latitude, this.zoom);
    }
});

/* ========== S P H E R I C A L   C O O R D S   C L A S S ========== */
var SphericalCoords = Class.create(Coords, {
    initialize : function($super, first, second, zoom) {
        $super('spherical', first, second, zoom);
    },

    toOSM : function() {
        var x = Math.floor((this.first + 180) / 360 * Math.pow(2, this.zoom)),
            y = Math.floor((1 - Math.log(Math.tan(this.second * Math.PI / 180) + 1
                / Math.cos(this.second * Math.PI / 180)) / Math.PI) / 2 * Math.pow(2, this.zoom));
        return new OSMCoords(x, y, this.zoom);
    },

    toCartesian : function(moveX, moveY) {
        moveX = typeof moveX != 'undefined' ? moveX : 0;
        moveY = typeof moveY != 'undefined' ? moveY : 0;
        var x = Math.floor((this.first + 180) / 360 * Math.pow(2, this.zoom)),
            y = Math.floor((1 - Math.log(Math.tan(this.second * Math.PI / 180) + 1
                / Math.cos(this.second * Math.PI / 180)) / Math.PI) / 2 * Math.pow(2, this.zoom));
        return new CartesianCoords(256 * (x + moveX - 65532), 256 * (43598 - (y + moveY)), this.zoom);
    },

    toSpherical : function() {
        return this;
    }
});
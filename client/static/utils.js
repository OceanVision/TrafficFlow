/* ========== L I N E A R   F U N C T I O N   C L A S S ========== */
var LinearFunction = Class.create({
    initialize : function(a, b, domain) {
        this.a = a;
        this.b = b;
        this.domain = domain;
    },

    f : function(x) {
        if (x >= this.domain[0] && x <= this.domain[1]) {
            return this.a * x + this.b;
        }
        return null;
    },

    fInv : function(y) {
        var x = (y - this.b) / this.a;
        if (x >= this.domain[0] && x <= this.domain[1]) {
            return x;
        }
        return null;
    }
});

/* ========== M A R K E R   C L A S S ========== */
var Marker = Class.create({
    initialize : function(sphericalCoords) { // coords muszą być sferyczne, inaczej to nie ma sensu
        this.coords = sphericalCoords;
        this.size = 16;
    },

    getPositionInTile : function() {
        var spherical1 = this.coords.toOSM().toSpherical(),
            spherical2 = this.coords.toOSM(1, 1).toSpherical();
        return {
            x: ((this.coords.first - spherical1.first) /
                (spherical2.first - spherical1.first)) * 256 - this.size / 2,
            y: ((this.coords.second - spherical1.second) /
                (spherical2.second - spherical1.second)) * 256 - this.size / 2
        };
    },

    toElement : function() {
        var marker = jQuery('<div class="marker"></div>'),
            position = this.getPositionInTile();
        marker.css({
            left: position.x + 'px',
            top: position.y + 'px'
        });
        return marker;
    }
});

/* ========== L O C A T I O N   M A R K E R   C L A S S ========== */
var LocationMarker = Class.create(Marker, {
    toElement : function() {
        var marker = jQuery('<div class="marker location"></div>'),
            position = this.getPositionInTile();
        marker.css({
            left: position.x + 'px',
            top: position.y + 'px'
        });
        return marker;
    }
});

/* ========== T I L E   C L A S S ========== */
var Tile = Class.create({
    tilesServerURL : 'http://c.tile.openstreetmap.org',

    initialize : function(coords) {
        this.coords = coords.toOSM();
        var img = jQuery('<img/>').attr('src', this.tilesServerURL + '/'
                + this.coords.zoom + '/' + this.coords.first + '/' + this.coords.second + '.png'),
            canvas = jQuery('<canvas width="256" height="256"></canvas>');

        this.element = jQuery('<div class="tile"></div>');
        this.element
            .append(img)
            .append(canvas);

        this.element.off('dblclick').on('dblclick', (function(e) {
            if (main.username == null) {
                return;
            }

            var posX = e.pageX - this.element.offset().left,
                posY = e.pageY - this.element.offset().top,
                coords = this.coords.toCartesian(posX, -posY).toSpherical(),
                data = 'longitude=' + coords.first + '&latitude=' + coords.second,
                tile = this;

            ajax.request('add_marker', 'GET', data, function(response) {
                if (response == 'ok') {
                    tile.appendMarker(new Marker(coords));
                }
            });
        }).bind(this));

        this.img = img[0];
        this.img.onload = (function() {
            this.drawStreetsLines();
            this.element.animate({
                opacity: 1
            }, 500);
        }).bind(this);

        this.streetsLines = [];
        this.markers = [];
    },

    appendMarker : function(marker) {
        marker
            .toElement()
            .appendTo(this.element)
            .show();
    },

    setMarkers : function() {
        var tileTask = drawingTasks.getTileTask(this.coords);
        if (tileTask == null) {
            return;
        }

        this.markers = tileTask.getTasksByType('marker');
        for (var i = 0; i < this.markers.length; i++) {
            this.appendMarker(this.markers[i].data);
        }
    },

    drawStreetsLines : function() {
        var tileTask = drawingTasks.getTileTask(this.coords);
        if (tileTask == null) {
            return;
        }
        var ctx = this.element.find('canvas')[0].getContext('2d');

        this.streetsLines = tileTask.getTasksByType('streets-line');
        for (var i = 0; i < this.streetsLines.length; i++) {
            ctx.strokeStyle = '#' + this.streetsLines[i].data.colour;
            ctx.beginPath();
            ctx.lineWidth = this.streetsLines[i].data.width;
            ctx.moveTo(this.streetsLines[i].data.vertexes[0].x, this.streetsLines[i].data.vertexes[0].y);
            ctx.lineTo(this.streetsLines[i].data.vertexes[1].x, this.streetsLines[i].data.vertexes[1].y);
            ctx.stroke();
        }
    }
});

/* ========== T A S K   C L A S S ========== */
var Task = Class.create({
    initialize : function(type, data) {
        this.type = type;
        this.data = data;
    }
});

/* ========== T I L E   T A S K S   C L A S S ========== */
var TileTasks = Class.create({
    initialize : function(coords, initialTask) {
        this.coords = coords.toOSM();
        this.tasks = [initialTask];
    },

    getTasksByType : function(type) {
        var tasks = [];
        for (var i = 0; i < this.tasks.length; i++) {
            if (this.tasks[i].type == type) {
                tasks.push(this.tasks[i]);
            }
        }
        return tasks;
    }
});

/* ========== D R A W I N G   T A S K S   C L A S S ========== */
var DrawingTasks = Class.create({
    initialize : function() {
        this.tileTasks = [];
    },

    getTileTask : function(coords) {
        for (var i = 0; i < this.tileTasks.length; i++) {
            if (this.tileTasks[i].coords.compare(coords.toOSM())) {
                return this.tileTasks[i];
            }
        }
        return null;
    },

    updateTileTask : function(coords, task) {
        var foundTileTask = this.getTileTask(coords);
        if (foundTileTask == null) {
            this.tileTasks.push(new TileTasks(coords, task));
            return this.tileTasks[this.tileTasks.length - 1];
        } else {
            foundTileTask.tasks.push(task);
            return foundTileTask;
        }
    }
});
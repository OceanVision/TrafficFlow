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

/* ========== P O I N T E R   C L A S S ========== */
var Pointer = Class.create({
    initialize : function(coords) { // coords muszą być sferyczne, inaczej to nie ma sensu
        this.coords = coords;
        this.size = 16;
    },

    getPositionInTile : function() {
        var osm = this.coords.toOSM(),
            spherical = osm.toSpherical();
        osm.first++;
        osm.second++;
        var sphericalNext = osm.toSpherical();
        return {
            x: ((this.coords.first - spherical.first) /
                (sphericalNext.first - spherical.first)) * 256 - this.size / 2,
            y: ((this.coords.second - spherical.second) /
                (sphericalNext.second - spherical.second)) * 256 - this.size / 2
        };
    },

    toElement : function() {
        var pointer = jQuery('<div class="pointer"></div>'),
            position = this.getPositionInTile();
        pointer.css({
            left: position.x + "px",
            top: position.y + "px"
        });
        return pointer;
    }
});

/* ========== L O C A T I O N   P O I N T E R   C L A S S ========== */
var LocationPointer = Class.create(Pointer, {
    toElement : function() {
        var pointer = jQuery('<div class="pointer location"></div>'),
            position = this.getPositionInTile();
        pointer.css({
            left: position.x + "px",
            top: position.y + "px"
        });
        return pointer;
    }
});

/* ========== T I L E   C L A S S ========== */
var Tile = Class.create({
    tilesServerURL : 'http://c.tile.openstreetmap.org',

    initialize : function(coords) {
        this.coords = coords.toOSM();
        var img = jQuery('<img/>').attr("src", this.tilesServerURL + "/"
                + this.coords.zoom + "/" + this.coords.first + "/" + this.coords.second + ".png"),
            canvas = jQuery('<canvas width="256" height="256"></canvas>');

        this.element = jQuery('<div class="tile"></div>');
        this.element
            .append(img)
            .append(canvas);

        this.img = img[0];
        this.img.onload = (function() {
            this.drawStreetsLines();
            this.appendPointers();
            this.element.animate({
                opacity: 1
            }, 500);
        }).bind(this);

        this.pointers = [];
    },

    setPointers : function(pointers) {
        for (var i = 0; i < pointers.length; i++) {
            if (this.coords.compare(pointers[i].osm)) {
                this.pointers.push(pointers[i].pointer);
            }
        }
    },

    appendPointers : function() {
        for (var i = 0; i < this.pointers.length; i++) {
            this.pointers[i]
                .toElement()
                .appendTo(this.element)
                .show();
        }
    },

    drawStreetsLines : function() {
        var task = drawingTasks.getTask(this.coords);
        if (task == null) {
            return;
        }

        var lines = task.lines,
            ctx = this.element.find('canvas')[0].getContext('2d');

        for (var i = 0; i < lines.length; i++) {
            ctx.strokeStyle = '#' + lines[i].colour;
            ctx.beginPath();
            ctx.lineWidth = lines[i].width;
            ctx.moveTo(lines[i].points[0].x, lines[i].points[0].y);
            ctx.lineTo(lines[i].points[1].x, lines[i].points[1].y);
            ctx.stroke();
        }
    }
});

/* ========== T I L E   D A T A   C L A S S ========== */
var TileData = Class.create({
    initialize : function(coords, line) {
        this.coords = coords.toOSM();
        this.lines = [line];
    }
});

/* ========== D R A W I N G   T A S K S   C L A S S ========== */
var DrawingTasks = Class.create({
    initialize : function() {
        this.tasks = [];
    },

    pushTask : function(coords, line) {
        var found = -1;
        for (var i = 0; i < this.tasks.length; i++) {
            if (this.tasks[i].coords.compare(coords.toOSM())) {
                found = i;
                break;
            }
        }

        if (found == -1) {
            this.tasks.push(new TileData(coords, line));
        } else {
            this.tasks[i].lines.push(line);
        }
    },

    getTask : function(coords) {
        for (var i = 0; i < this.tasks.length; i++) {
            if (this.tasks[i].coords.compare(coords.toOSM())) {
                return this.tasks[i];
            }
        }
        return null;
    }
});
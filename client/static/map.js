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
        ctx.strokeStyle = '#FF0000';
        for (var i = 0; i < lines.length; i++) {
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

/* ========== M A P   C L A S S ========== */
var Map = Class.create({
    content : null,
    tiles : null,
    coords : null,

    buffer : {
        width : null,
        height : null
    },

    viewport : {
        x : 0,
        y : 0,
        width : null,
        height : null,
        coeficient : 512
    },

    pointers : [],

    initialize : function() {
        this.content = jQuery("#map");
        //this.coords = new SphericalCoords(19.93731, 50.06184, 17);
        this.coords = new OSMCoords(72794, 44417, 17);
    },

    start : function() {
        this.buffer.width = Math.round(jQuery(window).width() / 256) + 2;
        this.buffer.height = Math.round(jQuery(window).height() / 256) + 2;

        this.viewport.x = jQuery(window).width() / 2;
        this.viewport.y = jQuery(window).height() / 2;
        this.viewport.width = 256 * this.buffer.width;
        this.viewport.height = 256 * this.buffer.height;

        this.tiles = new Array(this.buffer.width);
        for (var i = 0; i < this.buffer.width; ++i) {
            this.tiles[i] = new Array(this.buffer.height);
        }

        geolocation.getLocation(function(coords, accuracy) {
            map.coords = coords.toOSM();

            // Lokacja
            map.pointers.push({
                osm : coords.toOSM(),
                pointer : new LocationPointer(coords)
            });

            // TESTY
            var node1 = graph.addNode(new SphericalCoords(19.90606, 50.02688, 17));
            var node2 = graph.addNode(new SphericalCoords(19.91291, 50.02975, 17));
            graph.addLine(node1, node2, 2);

            var node3 = graph.addNode(new SphericalCoords(19.91295, 50.02976, 17));
            graph.addLine(node2, node3);

            var node4 = graph.addNode(new SphericalCoords(19.91471, 50.02806, 17));
            graph.addLine(node2, node4);

            var node5 = graph.addNode(new SphericalCoords(19.91474, 50.02808, 17));
            graph.addLine(node3, node5);

            var node6 = graph.addNode(new SphericalCoords(19.91449, 50.02798, 17));
            var node7 = graph.addNode(new SphericalCoords(19.91316, 50.02740, 17));
            var node8 = graph.addNode(new SphericalCoords(19.91288, 50.02719, 17));
            var node9 = graph.addNode(new SphericalCoords(19.91276, 50.02703, 17));
            var node10 = graph.addNode(new SphericalCoords(19.91272, 50.02690, 17));
            var node11 = graph.addNode(new SphericalCoords(19.91271, 50.02671, 17));
            graph.addLine(node5, node6);
            graph.addLine(node6, node7);
            graph.addLine(node7, node8);
            graph.addLine(node8, node9);
            graph.addLine(node9, node10);
            graph.addLine(node10, node11);
            // KONIEC TESTÓW

            main.showInfo("Your location has been retrieved (accuracy: " + accuracy + " metres).");
            map.load();
            map.update();
            map.addMapEvents();
        });
    },

    load : function() {
        graph.DFS();
        for (var i = 0; i < this.buffer.width; ++i) {
            for (var j = 0; j < this.buffer.height; ++j) {
                this.tiles[i][j] =
                    new Tile(new OSMCoords(i + this.coords.first, j + this.coords.second, this.coords.zoom));
                this.tiles[i][j].element.css({
                    left : (i * 256 + this.viewport.x) + "px",
                    top : (j * 256 + this.viewport.y) + "px"
                });
                this.tiles[i][j].setPointers(this.pointers);
                this.content.append(this.tiles[i][j].element);
            }
        }
    },

    update : function() {
        setTimeout(this.updateLeft.bind(this), 0);
        setTimeout(this.updateTop.bind(this), 0);
        setTimeout(this.updateRight.bind(this), 0);
        setTimeout(this.updateBottom.bind(this), 0);
    },

    updateLeft : function() {
        while (this.viewport.x > 0) {
            this.coords.first--;
            this.tiles.splice(0, 0, new Array(this.buffer.height));

            for (var j = 0; j < this.buffer.height; ++j) {
                this.tiles[0][j] =
                    new Tile(new OSMCoords(this.coords.first, j + this.coords.second, this.coords.zoom));
                this.tiles[0][j].element.css({
                    left : (this.viewport.x - 256) + "px",
                    top : (j * 256 + this.viewport.y) + "px"
                });
                this.tiles[0][j].setPointers(this.pointers);
                this.content.append(this.tiles[0][j].element);
                this.tiles[this.tiles.length - 1][j].element.detach();
            }

            this.tiles.splice(this.tiles.length - 1, 1);
            this.viewport.x -= 256;
        }
    },

    updateTop : function() {
        while (this.viewport.y > 0) {
            this.coords.second--;

            for (var i = 0; i < this.buffer.width; ++i) {
                this.tiles[i].splice(0, 0, "");
                this.tiles[i][0] =
                    new Tile(new OSMCoords(i + this.coords.first, this.coords.second, this.coords.zoom));
                this.tiles[i][0].element.css({
                    left : (i * 256 + this.viewport.x) + "px",
                    top : (this.viewport.y - 256) + "px"
                });
                this.tiles[i][0].setPointers(this.pointers);
                this.content.append(this.tiles[i][0].element);
                this.tiles[i][this.tiles[i].length - 1].element.detach();
                this.tiles[i].splice(this.tiles[i].length - 1, 1);
            }

            this.viewport.y -= 256;
        }
    },

    updateRight : function() {
        while (this.viewport.x + this.viewport.width < jQuery(window).width()) {
            this.coords.first++;
            this.tiles.push(new Array(this.buffer.height));

            for (var j = 0; j < this.buffer.height; ++j) {
                var i = this.tiles.length - 1;
                this.tiles[i][j] =
                    new Tile(new OSMCoords(this.coords.first + this.buffer.width - 1,
                        j + this.coords.second, this.coords.zoom));
                this.tiles[i][j].element.css({
                    left : (this.viewport.x + this.viewport.width) + "px",
                    top : (j * 256 + this.viewport.y) + "px"
                });
                this.tiles[i][j].setPointers(this.pointers);
                this.content.append(this.tiles[i][j].element);
                this.tiles[0][j].element.detach(); //wali sie
            }

            this.tiles.splice(0, 1);
            this.viewport.x += 256;
        }
    },

    updateBottom : function() {
        while (this.viewport.y + this.viewport.height < jQuery(window).height()) {
            this.coords.second++;

            for (var i = 0; i < this.buffer.width; ++i) {
                this.tiles[i].push("");
                var j = this.tiles[i].length - 1;
                this.tiles[i][j] =
                    new Tile(new OSMCoords(i + this.coords.first,
                        this.coords.second + this.buffer.height - 1, this.coords.zoom));
                this.tiles[i][j].element.css({
                    left : (i * 256 + this.viewport.x) + "px",
                    top : (this.viewport.y + this.viewport.height) + "px"
                });
                this.tiles[i][j].setPointers(this.pointers);
                this.content.append(this.tiles[i][j].element);
                this.tiles[i][0].element.detach();
                this.tiles[i].splice(0, 1);
            }

            this.viewport.y += 256;
        }
    },

    addMapEvents : function() {
        var mouse = {
            x : null,
            y : null,
            state : false
        };

        this.content.on("mousedown", function(e) {
            mouse.x = e.pageX;
            mouse.y = e.pageY;
            mouse.state = true;
        });

        this.content.on("mousemove", function(e) {
            if (!mouse.state) {
                return;
            }

            jQuery(".tile").css({
                left : "+=" + (e.pageX - mouse.x),
                top : "+=" + (e.pageY - mouse.y)
            });

            mouse.x = e.pageX;
            mouse.y = e.pageY;
        });

        this.content.on("mouseup mouseleave", function(e) {
            mouse.state = false;
            map.viewport.x = map.tiles[0][0].element.position().left;
            map.viewport.y = map.tiles[0][0].element.position().top;
            map.update();
        });
    }
});
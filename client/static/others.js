var popupAvailableId = 0;

/* ========== P O P U P   C L A S S ========== */
var Popup = Class.create({
    initialize : function(type, sphericalCoords) {
        this.id = popupAvailableId;
        popupAvailableId++;
        this.type = type;
        this.coords = sphericalCoords;
        this.element = this.toElement();
        this.tile = null;

        this.element
            .on('click', function() {
                if (map.activePopup != null) {
                    map.activePopup.detach();
                }
            })
            .on('mousedown mousemove mouseup', function(e) {
                e.stopPropagation();
            });
    },

    getPositionInTile : function() {
        var spherical1 = this.coords.toOSM().toSpherical(),
            spherical2 = this.coords.toOSM(1, 1).toSpherical();
        return {
            x: ((this.coords.first - spherical1.first) /
                (spherical2.first - spherical1.first)) * 256 + 20,
            y: ((this.coords.second - spherical1.second) /
                (spherical2.second - spherical1.second)) * 256 - 10
        };
    },

    toElement : function() {
        var popup = jQuery('<div class="popup ' + this.type + '">Click another marker to find the route.</div>'),
            position = this.getPositionInTile();
        popup.css({
            left: position.x + 'px',
            top: position.y + 'px'
        });
        return popup;
    },

    attachToTile : function(tile) {
        if (map.activePopup != null) {
            map.activePopup.detach();
        }
        this.tile = tile;
        this.element
            .appendTo(this.tile.element)
            .show();
        this.tile.popup = this;
        map.activePopup = this;
    },

    detach : function() {
        this.element
            .detach();
        this.tile.popup = null;
        map.activePopup = null;
        map.activeMarker = null;
    }
});

var markerAvailableId = 0;

/* ========== M A R K E R   C L A S S ========== */
var Marker = Class.create({
    initialize : function(sphericalCoords) {
        this.id = markerAvailableId;
        markerAvailableId++;
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
        marker
            .css({
                left: position.x + 'px',
                top: position.y + 'px'
            })
            .on('dblclick', function(e) {
                e.stopPropagation();
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

        this.element.on('dblclick', (function(e) {
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
        this.popup = null;
    },

    appendMarker : function(marker) {
        var tile = this;
        marker.toElement()
            .appendTo(this.element)
            .show()
            .on('click', function(e) {
                e.stopPropagation();
                if (map.activeMarker != null && map.activeMarker.id == marker.id) {
                    tile.popup.detach();
                } else if (map.activeMarker == null) {
                    (new Popup('find-route', marker.coords)).attachToTile(tile);
                    map.activeMarker = marker;
                } else {
                    var startNode = graph.findClosestNode(map.activeMarker.coords),
                        endNode = graph.findClosestNode(marker.coords);

                    var route = graph.findBestRoute(startNode.id, endNode.id);
                    for (var i = 0; i < route.length-1; i++) {
                        graph.addLineToRoute(route[i], route[i+1]);
                    }
                    graph.DFS();
                    map.redrawStreetsGraph();
                    map.activePopup.detach();
                    map.activeMarker = null;
                }
            });
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
        ctx.clearRect (0, 0, 256, 256);

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
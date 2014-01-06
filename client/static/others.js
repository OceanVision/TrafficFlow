var popupAvailableId = 0;

/* ========== P O P U P   C L A S S ========== */
var Popup = Class.create({
    initialize : function(type, sphericalCoords, tile, data) {
        this.id = popupAvailableId;
        popupAvailableId++;
        this.type = type;
        this.coords = sphericalCoords;
        this.tile = tile;
        this.data = typeof data != 'undefined' ? data : null;
        this.element = this.toElement();
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
        var element, position, self = this;
        jQuery.ajaxSetup({
            async: false
        });
        jQuery.get('get_popup/' + this.type, function(response) {
            element = jQuery(response);
            if (self.type == 'add-marker') {
                element.find('#id_longitude').val(self.coords.first);
                element.find('#id_latitude').val(self.coords.second);
            }
        });

        position = this.getPositionInTile();
        element.css({
            left: position.x + 'px',
            top: position.y + 'px'
        });
        this.addEvents(element);
        return element;
    },

    attach : function() {
        if (map.activePopup != null) {
            map.activePopup.detach();
        }
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
        if (this.type == 'add-marker' && typeof this.data != 'undefined') {
            this.data.detach();
        }
    },

    addEvents : function(element) {
        var self = this;
        if (this.type == 'start-routing') {
            element.on('click', function(e) {
                e.stopPropagation();
                if (map.activePopup != null) {
                    jQuery.get('remove_marker', 'id=' + self.data.id, function(response) {
                        if (response == 'ok') {
                            utils.showInfo('The marker was removed successfully.');
                            self.data.detach();
                            self.data = undefined;
                        } else {
                            utils.showInfo('The marker could not be removed.');
                        }
                        map.activePopup.detach();
                    });
                }
            });
        } else if (this.type == 'add-marker') {
            element.find('form').submit(function() {
                jQuery.post('get_popup/' + self.type, $(this).serialize(), function(response) {
                    if (response != 'fail') {
                        utils.showInfo('The marker was added successfully.');
                        self.data.id = parseInt(response);
                        self.data = undefined;
                    } else {
                        utils.showInfo('The marker could not be added.');
                    }
                    self.detach()
                });
                return false;
            });
        }

        element.find('input.cancel').on('click', function(e) {
            e.stopPropagation();
            self.detach();
        });

        element.on('mousedown mousemove mouseup', function(e) {
            e.stopPropagation();
        });
    }
});

/* ========== M A R K E R   C L A S S ========== */
var Marker = Class.create({
    initialize : function(sphericalCoords, tile, id) {
        this.id = typeof id != 'undefined' ? id : null;
        this.coords = sphericalCoords;
        this.size = 16;
        this.tile = tile;
        this.element = this.toElement();
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
        var element = jQuery('<div class="marker"></div>'),
            position = this.getPositionInTile();

        element.css({
                left: position.x + 'px',
                top: position.y + 'px'
            });
        this.addEvents(element);
        return element;
    },

    attach : function() {
        this.element
            .appendTo(this.tile.element)
            .show();
    },

    detach : function() {
        this.element
            .detach();
    },

    addEvents : function(element) {
        var self = this;
        element
            .on('click', function(e) {
                e.stopPropagation();

                if (map.activeMarker != null && map.activeMarker.id == self.id) {
                    self.tile.popup.detach();
                } else if (map.activeMarker == null) {
                    (new Popup('start-routing', self.coords, self.tile, self)).attach();
                    map.activeMarker = self;
                } else {
                    var startNode = graph.findClosestNode(map.activeMarker.coords),
                        endNode = graph.findClosestNode(self.coords);

                    var route = graph.findBestRoute(startNode.id, endNode.id);
                    for (var i = 0; i < route.length-1; i++) {
                        graph.addLineToRoute(route[i], route[i+1]);
                    }
                    graph.DFS();
                    map.redrawStreetsGraph();
                    map.activePopup.detach();
                    map.activeMarker = null;
                }
            })
            .on('dblclick', function(e) {
                e.stopPropagation();
            });
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
            if (utils.activeUsername == null) {
                return;
            }

            var posX = e.pageX - this.element.offset().left,
                posY = e.pageY - this.element.offset().top,
                coords = this.coords.toCartesian(posX, -posY).toSpherical(),
                data = 'longitude=' + coords.first + '&latitude=' + coords.second,
                tile = this;

            var marker = new Marker(coords, this);
            (new Popup('add-marker', coords, this, marker)).attach();
            marker.attach();
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

    setMarkers : function() {
        var tileTask = drawingTasks.getTileTask(this.coords);
        if (tileTask == null) {
            return;
        }

        this.markers = tileTask.getTasksByType('marker');
        for (var i = 0; i < this.markers.length; i++) {
            (new Marker(this.markers[i].data.coords, this, this.markers[i].data.id)).attach();
        }

        var locationMarker = tileTask.getTasksByType('location-marker');
        if (locationMarker.length == 1) {
            this.markers.push(locationMarker[0]);
            (new LocationMarker(locationMarker[0].data.coords, this, locationMarker[0].data.id)).attach();
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
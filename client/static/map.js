/* ========== M A P   C L A S S ========== */
var Map = Class.create({
    content : null,
    tiles : null,
    startingCoords : null,

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

    activeMarker : null,
    activePopup : null,

    initialize : function() {
        this.content = jQuery('#map');
        //this.startingCoords = new SphericalCoords(19.93731, 50.06184, 17);
        this.startingCoords = new OSMCoords(72794, 44417, 17);
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

        geolocation.getLocation(function(locationCoords, accuracy) {
            map.startingCoords = locationCoords.toOSM();

            // Lokalizacja
            drawingTasks.updateTileTask(locationCoords, new Task('marker', new LocationMarker(locationCoords)));

            // Pobiera ulubione punkty z bazy danych
            ajax.getJSON('get_markers', function(data) {
                for (var i = 0; i < data.markers.length; i++) {
                    var coords = new SphericalCoords(data.markers[i].longitude, data.markers[i].latitude, 17);
                    drawingTasks.updateTileTask(coords, new Task('marker', new Marker(coords)));
                }
            });

            // Pobiera graf z bazy danych
            ajax.getJSON('get_graph', function(data) {
                for (var i = 0; i < data.nodes.length; i++) {
                    graph.addNode(data.nodes[i].id,
                        new SphericalCoords(data.nodes[i].longitude, data.nodes[i].latitude, 17));
                }

                for (var i = 0; i < data.lines.length; i++) {
                    graph.addLine(data.lines[i].startNodeId, data.lines[i].endNodeId, data.lines[i].ways);
                }

                graph.DFS();
            });

            map.load();
            map.update();
            map.addMapEvents();
            utils.showInfo('Your location has been retrieved (accuracy: ' + accuracy + ' metres).');
        }, function(error) {
            map.load();
            map.update();
            map.addMapEvents();
            utils.showInfo('Your location could not be retrieved.');
        });
    },

    load : function() {
        for (var i = 0; i < this.buffer.width; ++i) {
            for (var j = 0; j < this.buffer.height; ++j) {
                this.tiles[i][j] =
                    new Tile(new OSMCoords(i + this.startingCoords.first, j + this.startingCoords.second, this.startingCoords.zoom));
                this.tiles[i][j].element.css({
                    left : (i * 256 + this.viewport.x) + 'px',
                    top : (j * 256 + this.viewport.y) + 'px'
                });
                this.tiles[i][j].setMarkers();
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
            this.startingCoords.first--;
            this.tiles.splice(0, 0, new Array(this.buffer.height));

            for (var j = 0; j < this.buffer.height; ++j) {
                this.tiles[0][j] =
                    new Tile(new OSMCoords(this.startingCoords.first, j + this.startingCoords.second, this.startingCoords.zoom));
                this.tiles[0][j].element.css({
                    left : (this.viewport.x - 256) + 'px',
                    top : (j * 256 + this.viewport.y) + 'px'
                });
                this.tiles[0][j].setMarkers();
                this.content.append(this.tiles[0][j].element);
                this.tiles[this.tiles.length - 1][j].element.detach();
            }

            this.tiles.splice(this.tiles.length - 1, 1);
            this.viewport.x -= 256;
        }
    },

    updateTop : function() {
        while (this.viewport.y > 0) {
            this.startingCoords.second--;

            for (var i = 0; i < this.buffer.width; ++i) {
                this.tiles[i].splice(0, 0, '');
                this.tiles[i][0] =
                    new Tile(new OSMCoords(i + this.startingCoords.first, this.startingCoords.second, this.startingCoords.zoom));
                this.tiles[i][0].element.css({
                    left : (i * 256 + this.viewport.x) + 'px',
                    top : (this.viewport.y - 256) + 'px'
                });
                this.tiles[i][0].setMarkers();
                this.content.append(this.tiles[i][0].element);
                this.tiles[i][this.tiles[i].length - 1].element.detach();
                this.tiles[i].splice(this.tiles[i].length - 1, 1);
            }

            this.viewport.y -= 256;
        }
    },

    updateRight : function() {
        while (this.viewport.x + this.viewport.width < jQuery(window).width()) {
            this.startingCoords.first++;
            this.tiles.push(new Array(this.buffer.height));

            for (var j = 0; j < this.buffer.height; ++j) {
                var i = this.tiles.length - 1;
                this.tiles[i][j] =
                    new Tile(new OSMCoords(this.startingCoords.first + this.buffer.width - 1,
                        j + this.startingCoords.second, this.startingCoords.zoom));
                this.tiles[i][j].element.css({
                    left : (this.viewport.x + this.viewport.width) + 'px',
                    top : (j * 256 + this.viewport.y) + 'px'
                });
                this.tiles[i][j].setMarkers();
                this.content.append(this.tiles[i][j].element);
                this.tiles[0][j].element.detach(); //wali sie
            }

            this.tiles.splice(0, 1);
            this.viewport.x += 256;
        }
    },

    updateBottom : function() {
        while (this.viewport.y + this.viewport.height < jQuery(window).height()) {
            this.startingCoords.second++;

            for (var i = 0; i < this.buffer.width; ++i) {
                this.tiles[i].push('');
                var j = this.tiles[i].length - 1;
                this.tiles[i][j] =
                    new Tile(new OSMCoords(i + this.startingCoords.first,
                        this.startingCoords.second + this.buffer.height - 1, this.startingCoords.zoom));
                this.tiles[i][j].element.css({
                    left : (i * 256 + this.viewport.x) + 'px',
                    top : (this.viewport.y + this.viewport.height) + 'px'
                });
                this.tiles[i][j].setMarkers();
                this.content.append(this.tiles[i][j].element);
                this.tiles[i][0].element.detach();
                this.tiles[i].splice(0, 1);
            }

            this.viewport.y += 256;
        }
    },

    redrawStreetsGraph : function() {
        for (var i = 0; i < this.buffer.width; ++i) {
            for (var j = 0; j < this.buffer.height; ++j) {
                this.tiles[i][j].drawStreetsLines();
            }
        }
    },

    addMapEvents : function() {
        var mouse = {
            x : null,
            y : null,
            state : false
        };

        this.content.off('mousedown').on('mousedown', function(e) {
            mouse.x = e.pageX;
            mouse.y = e.pageY;
            mouse.state = true;
        });

        this.content.off('mousemove').on('mousemove', function(e) {
            if (!mouse.state) {
                return;
            }

            jQuery('div.tile').css({
                left : '+=' + (e.pageX - mouse.x),
                top : '+=' + (e.pageY - mouse.y)
            });

            mouse.x = e.pageX;
            mouse.y = e.pageY;
        });

        this.content.off('mouseup mouseleave').on('mouseup mouseleave', function(e) {
            mouse.state = false;
            map.viewport.x = map.tiles[0][0].element.position().left;
            map.viewport.y = map.tiles[0][0].element.position().top;
            map.update();
        });
    }
});
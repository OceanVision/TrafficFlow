function Main() {
    'use strict';
    this.init();
}

(function() {
    'use strict';
    Main.prototype = {
        databaseURL : "http://a.tile.openstreetmap.org",
        content : null,
        tiles : null,
        isLocationAvailable : false,
        locationPointer : {
            handle : null,
            position : null
        },

        coordinates : {
            longitude : 19.93731,
            latitude : 50.06184,
            zoom : 17,
            x : null,
            y : null,
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

        getLocation : null,
        sphericalToCartesian : null,
        cartesianToSpherical : null,
        update: null,
        updateLeft : null,
        updateTop : null,
        updateRight : null,
        updateBottom : null,
        drawImage : null,
        getTile : null,
        getPointer : null,
        showInfo : null,
        addEvents : null
    };

    Main.prototype.init = function() {
        this.content = $("#map");
        this.setPositions();
    };

    Main.prototype.setPositions = function() {
        var self = this;
        $("body").children().each(function() {
            if ($(this).data("center-h")) {
                self.centerHorizontally($(this));
            }

            if ($(this).data("center-v")) {
                self.centerVertically($(this));
            }
        });
    };

    Main.prototype.centerHorizontally = function(element) {
        element.css("left", (($(window).width() - element.outerWidth()) / 2) + "px");
        return this;
    };

    Main.prototype.centerVertically = function(element) {
        element.css("top", (($(window).height() - element.outerHeight()) / 2) + "px");
        return this;
    };

    Main.prototype.startMap = function() {
        var cartesianCoordinates = this.sphericalToCartesian(
                this.coordinates.longitude,
                this.coordinates.latitude,
                this.coordinates.zoom);

        this.coordinates.x = cartesianCoordinates.x;
        this.coordinates.y = cartesianCoordinates.y;
        this.coordinates.width = Math.round($(window).width() / 256) + 2;
        this.coordinates.height = Math.round($(window).height() / 256) + 2;

        this.viewport.x = $(window).width() / 2;
        this.viewport.y = $(window).height() / 2;
        this.viewport.width = 256 * this.coordinates.width;
        this.viewport.height = 256 * this.coordinates.height;

        this.tiles = new Array(this.coordinates.width);
        for (var i = 0; i < this.coordinates.width; ++i) {
            this.tiles[i] = new Array(this.coordinates.height);
        }

        for (var i = 0; i < this.coordinates.width; ++i) {
            for (var j = 0; j < this.coordinates.height; ++j) {
                this.tiles[i][j] = this.getTile(i + this.coordinates.x,
                        j + this.coordinates.y)
                .css({
                    left : (i * 256 + this.viewport.x) + "px",
                    top : (j * 256 + this.viewport.y) + "px"
                });

                this.tiles[i][j].sphericalCoordinates = this.cartesianToSpherical(i + this.coordinates.x, j
                    + this.coordinates.y, this.coordinates.zoom);

                this.tiles[i][j].img.onload = this.drawImage.bind(this, this.tiles[i][j]);
                this.content.append(this.tiles[i][j]);
            }
        }

        if (this.isLocationAvailable) {
            this.locationPointer.handle = this.getPointer(true);
            this.locationPointer.handle
                .css({
                    left: this.locationPointer.position.x,
                    top: this.locationPointer.position.y
                })
                .appendTo(this.tiles[0][0])
                .show();
        }

        this.update();
        this.addEvents();
    };

    Main.prototype.getLocation = function() {
        var self = this;
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function(position) {
                self.coordinates.longitude = position.coords.longitude;
                self.coordinates.latitude = position.coords.latitude;

                var cartesiansTile = self.sphericalToCartesian(
                        self.coordinates.longitude,
                        self.coordinates.latitude,
                        self.coordinates.zoom);

                var sphericalTile = self.cartesianToSpherical(
                        cartesiansTile.x,
                        cartesiansTile.y,
                        self.coordinates.zoom);

                var sphericalTileNext = self.cartesianToSpherical(
                        cartesiansTile.x + 1,
                        cartesiansTile.y + 1,
                        self.coordinates.zoom);

                self.locationPointer.position = {
                    x: ((self.coordinates.longitude - sphericalTile.longitude) /
                            (sphericalTileNext.longitude - sphericalTile.longitude)) *
                            256,
                    y: ((self.coordinates.latitude - sphericalTile.latitude) /
                            (sphericalTileNext.latitude - sphericalTile.latitude)) *
                            256
                };

                self.isLocationAvailable = true;
                self.showInfo("Your location has been retrieved.");
            }, function(error) {
                self.locationPointer.handle.fadeOut(500);
                switch(error.code) {
                case error.PERMISSION_DENIED: {
                    self.showInfo("You denied to request for the location.");
                    break;
                }
                case error.POSITION_UNAVAILABLE: {
                    self.showInfo("The location information is unavailable.");
                    break;
                }
                case error.TIMEOUT: {
                    self.showInfo("A request for the location timed out.");
                    break;
                }
                case error.UNKNOWN_ERROR: {
                    self.showInfo("An unknown error occurred."); break;
                }
                };
                self.isLocationAvailable = false;
            });
        } else {
            this.locationPointer.handle.fadeOut(500);
            this.showInfo("Geolocation is not supported by this browser.");
            this.isLocationAvailable = false;
        };
    };

    Main.prototype.sphericalToCartesian = function(longitude, latitude, zoom) {
        return {
            x : Math.floor((longitude+180)/360*Math.pow(2,zoom)),
            y : Math.floor((1 - Math.log(Math.tan(latitude * Math.PI / 180) + 1
                    / Math.cos(latitude * Math.PI / 180))
                    / Math.PI)
                    / 2 * Math.pow(2, zoom))
        };
    };

    Main.prototype.cartesianToSpherical = function(x, y, zoom) {
        var n=Math.PI-2*Math.PI*y/Math.pow(2,zoom);
        return {
            longitude : x/Math.pow(2,zoom)*360-180,
            latitude : 180/Math.PI*Math.atan(0.5*(Math.exp(n)-Math.exp(-n)))
        };
    };

    Main.prototype.update = function() {
        // uwaga na async!
        setTimeout(this.updateLeft.bind(this), 0);
        setTimeout(this.updateTop.bind(this), 0);
        setTimeout(this.updateRight.bind(this), 0);
        setTimeout(this.updateBottom.bind(this), 0);
    };

    Main.prototype.updateLeft = function() {
        while (this.viewport.x > 0) {
            this.coordinates.x--;
            this.tiles.splice(0, 0, new Array(this.coordinates.height));

            for (var j = 0; j < this.coordinates.height; ++j) {
                this.tiles[0][j] = this.getTile(this.coordinates.x, j + this.coordinates.y)
                    .css({
                        left : (this.viewport.x - 256) + "px",
                        top : (j * 256 + this.viewport.y) + "px"
                    });
                this.tiles[0][j].img.onload = this.drawImage.bind(this, this.tiles[0][j]);
                this.content.append(this.tiles[0][j]);
                this.tiles[this.tiles.length - 1][j].detach();
            }

            this.tiles.splice(this.tiles.length - 1, 1);
            this.viewport.x -= 256;
        }
    };

    Main.prototype.updateTop = function() {
        while (this.viewport.y > 0) {
            this.coordinates.y--;

            for (var i = 0; i < this.coordinates.width; ++i) {
                this.tiles[i].splice(0, 0, "");
                this.tiles[i][0] = this.getTile(i + this.coordinates.x, this.coordinates.y)
                    .css({
                        left : (i * 256 + this.viewport.x) + "px",
                        top : (this.viewport.y - 256) + "px"
                    });
                this.tiles[i][0].img.onload = this.drawImage.bind(this, this.tiles[i][0]);
                this.content.append(this.tiles[i][0]);
                this.tiles[i][this.tiles[i].length - 1].detach();
                this.tiles[i].splice(this.tiles[i].length - 1, 1);
            }

            this.viewport.y -= 256;
        }
    };

    Main.prototype.updateRight = function() {
        while (this.viewport.x + this.viewport.width < $(window).width()) {
            this.coordinates.x++;
            this.tiles.push(new Array(this.coordinates.height));

            for (var j = 0; j < this.coordinates.height; ++j) {
                this.tiles[this.tiles.length - 1][j] =
                    this.getTile(this.coordinates.x + this.coordinates.width - 1, j + this.coordinates.y)
                    .css({
                        left : (this.viewport.x + this.viewport.width) + "px",
                        top : (j * 256 + this.viewport.y) + "px"
                    });
                this.tiles[this.tiles.length - 1][j].img.onload =
                    this.drawImage.bind(this, this.tiles[this.tiles.length - 1][j]);
                this.content.append(this.tiles[this.tiles.length - 1][j]);
                this.tiles[0][j].detach();
            }

            this.tiles.splice(0, 1);
            this.viewport.x += 256;
        }
    };

    Main.prototype.updateBottom = function() {
        while (this.viewport.y + this.viewport.height < $(window).height()) {
            this.coordinates.y++;

            for (var i = 0; i < this.coordinates.width; ++i) {
                this.tiles[i].push("");
                this.tiles[i][this.tiles[i].length - 1] =
                    this.getTile(i + this.coordinates.x, this.coordinates.y + this.coordinates.height - 1)
                    .css({
                        left : (i * 256 + this.viewport.x) + "px",
                        top : (this.viewport.y + this.viewport.height) + "px"
                    });
                this.tiles[i][this.tiles[i].length - 1].img.onload =
                    this.drawImage.bind(this, this.tiles[i][this.tiles[i].length - 1]);
                this.content.append(this.tiles[i][this.tiles[i].length - 1]);
                this.tiles[i][0].detach();
                this.tiles[i].splice(0, 1);
            }

            this.viewport.y += 256;
        }
    };

    Main.prototype.drawImage = function(tile) {
        tile.animate({
            opacity: 1
        }, 500);
    };

    Main.prototype.getTile = function(x, y) {
        var tile = $('<div class="tile"></div>');
        var img = $('<img/>').attr("src", this.databaseURL + "/"
                + this.coordinates.zoom + "/" + x + "/" + y + ".png");
        tile.append(img);
        tile.img = img[0];
        return tile;
    };

    Main.prototype.getPointer = function(isLocationPointer) {
        if (isLocationPointer) {
            return $('<div class="pointer location"></div>');
        } else {
            return $('<div class="pointer"></div>');
        }
    };

    Main.prototype.showInfo = function(text) {
        var element = $("div#info-box");

        element.text(text);
        this.centerHorizontally(element);
        element.fadeTo(1000, 0.9);

        setTimeout(function() {
            $("div#info-box").fadeOut(5000);
        }, 10000);
    };

    Main.prototype.addEvents = function() {
        var self = this, mouse = {
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

            $(".tile").css({
                left : "+=" + (e.pageX - mouse.x),
                top : "+=" + (e.pageY - mouse.y)
            });

            mouse.x = e.pageX;
            mouse.y = e.pageY;
        });

        this.content.on("mouseup mouseleave", function(e) {
            mouse.state = false;
            self.viewport.x = self.tiles[0][0].position().left;
            self.viewport.y = self.tiles[0][0].position().top;
            self.update();
        });
    };
}());

$(document).on("ready", function() {
    main = new Main();
});
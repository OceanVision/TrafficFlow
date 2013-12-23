/* ========== M A I N   C L A S S ========== */
var Main = Class.create({
    initialize : function() {
        this.setPositions();
    },

    setPositions : function() {
        var self = this;
        jQuery("body").children().each(function() {
            if (jQuery(this).data("center-h")) {
                self.centerHorizontally(jQuery(this));
            }

            if (jQuery(this).data("center-v")) {
                self.centerVertically(jQuery(this));
            }
        });
    },

    centerHorizontally : function(element) {
        element.css("left", ((jQuery(window).width() - element.outerWidth()) / 2) + "px");
        return this;
    },

    centerVertically : function(element) {
        element.css("top", ((jQuery(window).height() - element.outerHeight()) / 2) + "px");
        return this;
    },

    showInfo : function(text) {
        var element = jQuery("div#info-box");

        element.text(text);
        this.centerHorizontally(element);
        element.fadeTo(1000, 0.9);

        setTimeout(function() {
            jQuery("div#info-box").fadeOut(5000);
        }, 10000);
    }
});

jQuery(document).on("ready", function() {
    main = new Main();
    geolocation = new Geolocation();
    ajax = new Ajax();
    map = new Map();
    drawingTasks = new DrawingTasks();
    graph = new StreetsGraph();
});
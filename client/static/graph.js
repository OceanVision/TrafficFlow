/* ========== S T R E E T S   N O D E   C L A S S ========== */
var StreetsNode = Class.create({
    initialize : function(coords, title, description) { // coords muszą być sferyczne, inaczej to nie ma sensu
        this.coords = coords;
        this.title = title;
        this.description = description;
        this.outboundLines = [];
    },

    getPositionInTile : function() {
        var osm = this.coords.toOSM(),
            spherical = osm.toSpherical();
        osm.first++;
        osm.second++;
        var sphericalNext = osm.toSpherical();
        return {
            x: ((this.coords.first - spherical.first) /
                (sphericalNext.first - spherical.first)) * 256,
            y: ((this.coords.second - spherical.second) /
                (sphericalNext.second - spherical.second)) * 256
        };
    }
});

/* ========== S T R E E T S   L I N E   C L A S S ========== */
var StreetsLine = Class.create({
    initialize : function(node, ways, distance) {
        this.node = node;
        this.ways = ways;
        this.distance = distance;
        this.colour = false;
    }
});

/* ========== S T R E E T S   G R A P H   C L A S S ========== */
var StreetsGraph = Class.create({
    initialize : function(nodes) {
        var newNodes = typeof nodes != 'undefined' ? nodes.slice(0) : [];
        this.nodes = newNodes;
    },

    addNode : function(coords, title, description) {
        var newNode = new StreetsNode(coords, title, description);
        this.nodes.push(newNode);
        return newNode;
    },

    addLine : function(startNode, endNode, ways, distance) {
        ways = typeof ways != 'undefined' ? ways : 1;
        distance = typeof distance != 'undefined' ? distance : 1;
        startNode.outboundLines.push(new StreetsLine(endNode, ways, distance));
    },

    getCopy : function() {
        return new StreetsGraph(this.nodes);
    },

    // TODO: aktualnie jest problem z rysowaniem prawie pionowych linii
    detectStreetsLines : function(startNode, endNode, ways) {
        var startNodeCoords = startNode.coords.toCartesian(),
            endNodeCoords = endNode.coords.toCartesian(),
            startX, endX, startY, endY;

        var x1 = startNodeCoords.first + startNode.getPositionInTile().x,
            y1 = startNodeCoords.second - startNode.getPositionInTile().y,
            x2 = endNodeCoords.first + endNode.getPositionInTile().x,
            y2 = endNodeCoords.second - endNode.getPositionInTile().y,

            a = (y1 - y2) / (x1 - x2),
            b = y2 - a * x2,
            func = new LinearFunction(a, b, [new CartesianCoords(x1, y1, 17), new CartesianCoords(x2, y2, 17)]);

        if (startNodeCoords.toOSM().first < endNodeCoords.toOSM().first) {
            startX = startNodeCoords.toOSM().first;
            endX = endNodeCoords.toOSM().first;
        } else {
            startX = endNodeCoords.toOSM().first;
            endX = startNodeCoords.toOSM().first;
        }

        if (startNodeCoords.toOSM().second < endNodeCoords.toOSM().second) {
            startY = startNodeCoords.toOSM().second;
            endY = endNodeCoords.toOSM().second;
        } else {
            startY = endNodeCoords.toOSM().second;
            endY = startNodeCoords.toOSM().second;
        }

        for (var i = startX; i <= endX; i++) {
            for (var j = startY; j <= endY; j++) {
                var osm = new OSMCoords(i, j, 17),
                    lt = osm.toCartesian(),
                    rt = osm.toCartesian(1, 0),
                    lb = osm.toCartesian(0, 1),
                    rb = osm.toCartesian(1, 1),
                    singleLine = {
                        points : [],
                        width : 2 * ways
                    };

                if (lt.toOSM().compare(startNode.coords.toOSM())) {
                    singleLine.points.push(startNode.getPositionInTile());
                }

                if (lt.toOSM().compare(endNode.coords.toOSM())) {
                    singleLine.points.push(endNode.getPositionInTile());
                }

                if (func.fInv(lt.second) >= lt.first && func.fInv(lt.second) <= rt.first) {
                    singleLine.points.push({
                        x : func.fInv(lt.second) - lt.first,
                        y : 0
                    });
                }

                if (func.f(rt.first) >= rb.second && func.f(rt.first) <= rt.second) {
                    singleLine.points.push({
                        x : 256,
                        y : rt.second - func.f(rt.first)
                    });
                }

                if (func.fInv(lb.second) >= lb.first && func.fInv(lb.second) <= rb.first) {
                    singleLine.points.push({
                        x : func.fInv(lb.second) - lb.first,
                        y : 256
                    });
                }

                if (func.f(lt.first) >= lb.second && func.f(lt.first) <= lt.second) {
                    singleLine.points.push({
                        x : 0,
                        y : lt.second - func.f(lt.first)
                    });
                }

                if (singleLine.points.length == 2) {
                    drawingTasks.pushTask(new OSMCoords(i, j, 17), singleLine);
                }
            }
        }
    },

    visitNode : function(node) {
        for (var i = 0; i < node.outboundLines.length; i++) {
            if (!(node.outboundLines[i].colour)) {
                this.detectStreetsLines(node, node.outboundLines[i].node, node.outboundLines[i].ways);
                this.visitNode(node.outboundLines[i].node);
                node.outboundLines[i].colour = true;
            }
        }
    },

    DFS : function() {
        for (var i = 0; i < this.nodes.length; i++) {
            for (var j = 0; j < this.nodes[i].outboundLines.length; j++) {
                this.nodes[i].outboundLines[j].colour = false;
            }
        }

        for (var i = 0; i < this.nodes.length; i++) {
            this.visitNode(this.nodes[i]);
        }
    }
});
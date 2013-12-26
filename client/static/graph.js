/* ========== S T R E E T S   N O D E   C L A S S ========== */
var StreetsNode = Class.create({
    initialize : function(id, coords, title, description) { // coords muszą być sferyczne, inaczej to nie ma sensu
        this.id = id;
        this.coords = coords;
        this.title = title;
        this.description = description;
        this.outboundLines = [];
    },

    getPositionInTile : function() {
        var spherical1 = this.coords.toOSM().toSpherical(),
            spherical2 = this.coords.toOSM(1, 1).toSpherical();
        return {
            x: ((this.coords.first - spherical1.first) /
                (spherical2.first - spherical1.first)) * 256,
            y: ((this.coords.second - spherical1.second) /
                (spherical2.second - spherical1.second)) * 256
        };
    }
});

/* ========== S T R E E T S   L I N E   C L A S S ========== */
var StreetsLine = Class.create({
    initialize : function(node, colour, ways, distance) {
        this.node = node;
        this.colour = colour;
        this.ways = ways;
        this.distance = distance;
        this.isVisited = false;
    }
});

/* ========== S T R E E T S   G R A P H   C L A S S ========== */
var StreetsGraph = Class.create({
    initialize : function(nodes) {
        var newNodes = typeof nodes != 'undefined' ? nodes.slice(0) : [];
        this.nodes = newNodes;
    },

    addNode : function(id, coords, title, description) {
        title = typeof title != 'undefined' ? title : '';
        description = typeof description != 'undefined' ? description : '';

        if (this.findNodeById(id) != null) {
            return null;
        }

        var newNode = new StreetsNode(id, coords, title, description);
        this.nodes.push(newNode);
        return newNode;
    },

    addLine : function(startNodeId, endNodeId, ways, colour, distance) {
        ways = typeof ways != 'undefined' ? ways : 1;
        colour = typeof colour != 'undefined' ? colour : '555555';
        distance = typeof distance != 'undefined' ? distance : 1;

        var startNode = this.findNodeById(startNodeId),
            endNode = this.findNodeById(endNodeId);

        if (startNode == null || endNode == null) {
            return null;
        }

        var newLine = new StreetsLine(endNode, colour, ways, distance);
        startNode.outboundLines.push(newLine);
        return newLine;
    },

    findNodeById : function(nodeId) {
        for (var i = 0; i < this.nodes.length; i++) {
            if (this.nodes[i].id == nodeId) {
                return this.nodes[i];
            }
        }
        return null;
    },

    // TODO: aktualnie jest problem z rysowaniem prawie pionowych linii
    detectStreetsLines : function(startNode, line) {
        var endNode = line.node,
            startNodeCoords = startNode.coords.toCartesian(),
            endNodeCoords = endNode.coords.toCartesian(),
            startX, endX, startY, endY;

        var x1 = startNodeCoords.first,
            y1 = startNodeCoords.second,
            x2 = endNodeCoords.first,
            y2 = endNodeCoords.second,

            a = (y1 - y2) / (x1 - x2),
            b = y2 - a * x2,
            func = null;

        if (x1 <= x2) {
            func = new LinearFunction(a, b, [x1, x2]);
        } else {
            func = new LinearFunction(a, b, [x2, x1]);
        }

        if (startNodeCoords.toOSM().first <= endNodeCoords.toOSM().first) {
            startX = startNodeCoords.toOSM().first;
            endX = endNodeCoords.toOSM().first;
        } else {
            startX = endNodeCoords.toOSM().first;
            endX = startNodeCoords.toOSM().first;
        }

        if (startNodeCoords.toOSM().second <= endNodeCoords.toOSM().second) {
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
                    rt = osm.toCartesian(256, 0),
                    lb = osm.toCartesian(0, -256),
                    rb = osm.toCartesian(256, -256),
                    singleLine = {
                        vertexes : [],
                        width : 2 * line.ways,
                        colour : line.colour
                    };

                if (lt.toOSM().compare(startNode.coords.toOSM())) {
                    singleLine.vertexes.push(startNode.getPositionInTile());
                }

                if (lt.toOSM().compare(endNode.coords.toOSM())) {
                    singleLine.vertexes.push(endNode.getPositionInTile());
                }

                if (func.fInv(lt.second) >= lt.first && func.fInv(lt.second) <= rt.first) {
                    singleLine.vertexes.push({
                        x : func.fInv(lt.second) - lt.first,
                        y : 0
                    });
                }

                if (func.f(rt.first) >= rb.second && func.f(rt.first) <= rt.second) {
                    singleLine.vertexes.push({
                        x : 256,
                        y : rt.second - func.f(rt.first)
                    });
                }

                if (func.fInv(lb.second) >= lb.first && func.fInv(lb.second) <= rb.first) {
                    singleLine.vertexes.push({
                        x : func.fInv(lb.second) - lb.first,
                        y : 256
                    });
                }

                if (func.f(lt.first) >= lb.second && func.f(lt.first) <= lt.second) {
                    singleLine.vertexes.push({
                        x : 0,
                        y : lt.second - func.f(lt.first)
                    });
                }

                if (singleLine.vertexes.length == 2) {
                    var coords = new OSMCoords(i, j, 17);
                    drawingTasks.updateTileTask(coords, new Task('streets-line', singleLine));
                }
            }
        }
    },

    visitNode : function(node) {
        for (var i = 0; i < node.outboundLines.length; i++) {
            if (!(node.outboundLines[i].isVisited)) {
                this.detectStreetsLines(node, node.outboundLines[i]);
                this.visitNode(node.outboundLines[i].node);
                node.outboundLines[i].isVisited = true;
            }
        }
    },

    DFS : function() {
        for (var i = 0; i < this.nodes.length; i++) {
            for (var j = 0; j < this.nodes[i].outboundLines.length; j++) {
                this.nodes[i].outboundLines[j].isVisited = false;
            }
        }

        for (var i = 0; i < this.nodes.length; i++) {
            this.visitNode(this.nodes[i]);
        }
    }
});
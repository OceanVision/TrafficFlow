/* ========== T A S K   C L A S S ========== */
var Task = Class.create({
    initialize : function(type, data) {
        this.type = type;
        this.data = data;
    }
});

/* ========== T I L E   T A S K S   C L A S S ========== */
var TileTask = Class.create({
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
    },

    removeTasksOfType : function(type) {
        for (var i = this.tasks.length - 1; i >= 0; i--) {
            if (this.tasks[i].type == type) {
                this.tasks.splice(i, 1);
            }
        }
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
            this.tileTasks.push(new TileTask(coords, task));
            return this.tileTasks[this.tileTasks.length - 1];
        } else {
            foundTileTask.tasks.push(task);
            return foundTileTask;
        }
    },

    removeTasksOfType : function(type) {
        for (var i = 0; i < this.tileTasks.length; i++) {
            this.tileTasks[i].removeTasksOfType(type);
        }
    }
});
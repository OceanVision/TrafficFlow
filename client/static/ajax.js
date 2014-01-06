/* ========== A J A X   C L A S S ========== */
var Ajax = Class.create({
    request : function(path, type, data, successCallback, errorCallback) {
        errorCallback = typeof errorCallback != 'undefined' ? errorCallback : function() {};
        if (type == 'POST') {
            var csrftoken = ajax.getCookie('csrftoken');
            jQuery.ajaxSetup({
                beforeSend : function(xhr, settings) {
                    if (!ajax.csrfSafeMethod(settings.type) && ajax.sameOrigin(settings.url)) {
                        // Send the token to same-origin, relative URLs only.
                        // Send the token only if the method warrants CSRF protection
                        // Using the CSRFToken value acquired earlier
                        xhr.setRequestHeader('X-CSRFToken', csrftoken);
                    }
                }
            });
        }

        jQuery.ajax({
            url : 'http://' + location.host + '/' + path,
            global : false,
            type : type,
            data : data,
            dataType : 'html',
            async : false,
            success : function(response) {
                successCallback(response);
            },
            error : function() {
                errorCallback();
            }
        });
    },

    getJSON : function(path, data, successCallback) {
        jQuery.ajaxSetup({
            async: false
        });
        jQuery.getJSON('http://' + location.host + '/' + path, data, function(data) {
            successCallback(data);
        });
    },

    getCookie : function(name) {
        var cookieValue = null;
        if (document.cookie && document.cookie != '') {
            var cookies = document.cookie.split(';');
            for (var i = 0; i < cookies.length; i++) {
                var cookie = jQuery.trim(cookies[i]);
                // Does this cookie string begin with the name we want?
                if (cookie.substring(0, name.length + 1) == (name + '=')) {
                    cookieValue = decodeURIComponent(cookie.substring(name.length + 1));
                    break;
                }
            }
        }
        return cookieValue;
    },

    csrfSafeMethod : function(method) {
        // these HTTP methods do not require CSRF protection
        return (/^(GET|HEAD|OPTIONS|TRACE)$/.test(method));
    },

    sameOrigin : function(url) {
        // test that a given url is a same-origin URL
        // url could be relative or scheme relative or absolute
        var host = document.location.host; // host + port
        var protocol = document.location.protocol;
        var sr_origin = '//' + host;
        var origin = protocol + sr_origin;
        // Allow absolute or scheme relative URLs to same origin
        return (url == origin || url.slice(0, origin.length + 1) == origin + '/') ||
            (url == sr_origin || url.slice(0, sr_origin.length + 1) == sr_origin + '/') ||
            // or any other URL that isn't scheme relative or absolute i.e relative.
            !(/^(\/\/|http:|https:).*/.test(url));
    }
});

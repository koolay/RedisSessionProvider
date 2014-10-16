(function () {
    $.validator.addMethod('notequal', function (value, element, params) {
        var desInputId = $(element).attr('data-val-notequal-other');
        var desInputVal = $('#' + desInputId).first().val();
        return desInputVal != value;
    }, '');

    $.validator.unobtrusive.adapters.add('notequal', {}, function (options) {
        options.rules['notequal'] = true;
        options.messages['notequal'] = options.message;
    });
 
})(jQuery);
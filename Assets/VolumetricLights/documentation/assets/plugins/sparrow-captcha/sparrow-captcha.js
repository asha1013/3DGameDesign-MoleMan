jQuery(document).ready(function ($) {
    document.addEventListener('wpcf7invalid', function(event) {
        var invalidFields = event.detail.invalidFields;
    console.log(invalidFields);
        invalidFields.forEach(function(field) {
            if (field.name === 'sparrow_captcha_response') {
                document.getElementById('sparrow_warning').style.display = 'block';
            }
        });
    }, false);
});

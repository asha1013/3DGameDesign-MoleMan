(function ($) {
	$(function ($) {
		"use strict";
		$(document).on("submit", ".swp-form", function (ev) {
			ev.preventDefault();
			var form = $(this);

			form.find(
				".swp-email-valid-error, .swp-email-duplicate-error,.swp-success, .swp-raw-error"
			).hide();

			var subscribe_email = form.find(".field-email").val();
			var filter =
				/^\s*[\w\-\+_]+(\.[\w\-\+_]+)*\@[\w\-\+_]+\.[\w\-\+_]+(\.[\w\-\+_]+)*\s*$/;
			var valid = String(subscribe_email).search(filter) != -1;

			// Email check
			if (subscribe_email == "" || !valid) {
				form.find(".swp-email-valid-error").slideDown();
				return false;
			}

			// Validation
			var validationFailed = false;
			form.find(".swp-field").each(function () {
				var required = $(this).data("required");
				var value = $.trim($(this).val());
				var fieldname = $(this).attr("name");
				var type = $(this).attr("type");
				if (
					(type == "checkbox" || type == "radio") &&
					required == "1"
				) {
					var fieldChecked = false;
					form.find('input[name="' + fieldname + '"]').each(
						function () {
							if ($(this).is(":checked")) {
								fieldChecked = true;
							}
						}
					);

					var validationFailed = !fieldChecked
						? true
						: validationFailed;
					if (validationFailed) {
						$(this).parent().css("border", "1px solid red");
					}
				} else if (required == "1" && !value.length) {
					$(this).css("border-color", "red");
					validationFailed = true;
				}
			});
			if (validationFailed) {
				return false;
			}

			//Terms Check
			var $terms = form.find(".swp-terms");
			if ($terms.length > 0) {
				if (!$terms.prop("checked")) {
					alert("Please check checkbox");
					return false;
				}
			}
			// remove red validation borders
			form.find(".swp-field").each(function () {
				var required = $(this).data("required");
				var fieldname = $(this).attr("name");
				var type = $(this).attr("type");
				if (type == "checkbox" || type == "radio") {
					$(this).parent().css("border", "0px solid green");
				} else if (required == "1") {
					$(this).css("border-color", "green");
				}
			});

			// chekc if v3 recaptcha
			var recaptcha_field_id = form.find(".g-recaptcha-field-id").val();
			var g_recaptcha_sitekey = form.find(".g-recaptcha-sitekey").val();

			form.find(".swp-spinner").fadeIn();

			var form_data = form.serialize();

			var data = {
				action: "swp_form_submit",
				formdata: form_data,
			};

			$.post(swp.ajaxurl, data, function (result, textStatus, xhr) {
				form.find(".swp-spinner").fadeOut();
				var response = $.trim(result);
				if (response == "1") {
					$(document).trigger("swpFormSubmitSuccess");
					var redirect_url = form.find(".redirect_url").val();
					if (redirect_url != "") {
						window.location.href = redirect_url;
					} else {
						//no redirect url found
						form.find(".swp-success").slideDown();
						maybe_reset_recaptcha(
							recaptcha_field_id,
							g_recaptcha_sitekey
						);
						//reset the fields
						form.find(".field-name").val("");
						form.find(".field-email").val("");
					}
				} else if (response == "Already subscribed.") {
					form.find(".swp-email-duplicate-error").slideDown();
					maybe_reset_recaptcha(
						recaptcha_field_id,
						g_recaptcha_sitekey
					);
				} else if (response == "invalid-captcha") {
					form.find(".swp-captcha-error").slideDown();
					maybe_reset_recaptcha(
						recaptcha_field_id,
						g_recaptcha_sitekey
					);
				} else {
					form.find(".swp-raw-error").text(response).slideDown();
					maybe_reset_recaptcha(
						recaptcha_field_id,
						g_recaptcha_sitekey
					);
				}
			});
		});

		function maybe_reset_recaptcha(
			recaptcha_field_id,
			g_recaptcha_sitekey
		) {
			// v3
			if (typeof recaptcha_field_id != "undefined") {
				grecaptcha
					.execute(g_recaptcha_sitekey, { action: "contact" })
					.then(function (token) {
						var recaptchaResponse = document.getElementById(
							"recaptchaResponse-" + recaptcha_field_id
						);

						recaptchaResponse.value = token;
						// Make the Ajax call here
					});
			} else if (typeof grecaptcha != "undefined") {
				// v2
				grecaptcha.reset();
			}
		}
	});
})(jQuery);

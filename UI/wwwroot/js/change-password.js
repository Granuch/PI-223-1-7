$(document).ready(function () {
    function togglePasswordVisibility(inputId, iconId) {
        const passwordField = $(inputId);
        const eyeIcon = $(iconId);

        if (passwordField.attr('type') === 'password') {
            passwordField.attr('type', 'text');
            eyeIcon.removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            passwordField.attr('type', 'password');
            eyeIcon.removeClass('fa-eye-slash').addClass('fa-eye');
        }
    }

    $('#togglePassword').click(function () {
        togglePasswordVisibility('#newPassword', '#eyeIcon');
    });
    $('#toggleConfirmPassword').click(function () {
        togglePasswordVisibility('#confirmPassword', '#eyeIconConfirm');
    });

    function assessPasswordStrength(password) {
        let score = 0;
        let feedback = [];

        if (password.length >= 8) score += 1;
        else feedback.push('мінімум 8 символів');

        if (/[a-z]/.test(password)) score += 1;
        else feedback.push('малі літери');

        if (/[A-Z]/.test(password)) score += 1;
        else feedback.push('великі літери');

        if (/[0-9]/.test(password)) score += 1;
        else feedback.push('цифри');

        if (/[^A-Za-z0-9]/.test(password)) score += 1;
        else feedback.push('спеціальні символи');

        return { score, feedback };
    }

    $('#newPassword').on('input', function () {
        const password = $(this).val();
        const result = assessPasswordStrength(password);
        const score = result.score;
        const feedback = result.feedback;
        const percentage = (score / 5) * 100;

        const progressBar = $('#passwordStrength');
        const strengthText = $('#strengthText');

        progressBar.css('width', percentage + '%');

        if (score <= 2) {
            progressBar.removeClass().addClass('progress-bar bg-danger');
            strengthText.text('Слабкий').removeClass().addClass('text-danger');
        } else if (score <= 3) {
            progressBar.removeClass().addClass('progress-bar bg-warning');
            strengthText.text('Середній').removeClass().addClass('text-warning');
        } else if (score <= 4) {
            progressBar.removeClass().addClass('progress-bar bg-info');
            strengthText.text('Хороший').removeClass().addClass('text-info');
        } else {
            progressBar.removeClass().addClass('progress-bar bg-success');
            strengthText.text('Відмінний').removeClass().addClass('text-success');
        }

        if (feedback.length > 0 && password.length > 0) {
            strengthText.append(' (потрібно: ' + feedback.join(', ') + ')');
        }
    });

    $('#newPassword, #confirmPassword').on('input', function () {
        const password = $('#newPassword').val();
        const confirmPassword = $('#confirmPassword').val();
        const matchDiv = $('#passwordMatch');

        if (confirmPassword.length > 0) {
            if (password === confirmPassword) {
                $('#confirmPassword').removeClass('is-invalid').addClass('is-valid');
                matchDiv.html('<i class="fas fa-check text-success"></i> Паролі співпадають')
                    .removeClass('text-danger').addClass('text-success');
            } else {
                $('#confirmPassword').removeClass('is-valid').addClass('is-invalid');
                matchDiv.html('<i class="fas fa-times text-danger"></i> Паролі не співпадають')
                    .removeClass('text-success').addClass('text-danger');
            }
        } else {
            $('#confirmPassword').removeClass('is-invalid is-valid');
            matchDiv.html('');
        }
    });

    function generatePassword() {
        const length = parseInt($('#passwordLength').val());
        const includeSymbols = $('#includeSymbols').is(':checked');
        const includeNumbers = $('#includeNumbers').is(':checked');

        let charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (includeNumbers) charset += "0123456789";
        if (includeSymbols) charset += "!@#$%^&*()_+-=[]{}|;:,.<>?";

        let password = "";
        for (let i = 0; i < length; i++) {
            password += charset.charAt(Math.floor(Math.random() * charset.length));
        }
        return password;
    }

    $('#passwordLength').on('input', function () {
        $('#lengthValue').text($(this).val());
    });

    $('#generatePassword').click(function () {
        const generatedPassword = generatePassword();
        $('#generatedPassword').val(generatedPassword);
    });

    $('#useGenerated').click(function () {
        const generatedPassword = $('#generatedPassword').val();
        if (generatedPassword) {
            $('#newPassword').val(generatedPassword).trigger('input');
            $('#confirmPassword').val(generatedPassword).trigger('input');
            $(this).html('<i class="fas fa-check"></i> Використано')
                .removeClass('btn-success').addClass('btn-outline-success');
            setTimeout(() => {
                $(this).html('<i class="fas fa-check"></i> Використати')
                    .removeClass('btn-outline-success').addClass('btn-success');
            }, 2000);
        }
    });

    $('#copyGenerated').click(function () {
        const generatedPassword = $('#generatedPassword').val();
        const button = $(this);

        if (generatedPassword) {
            navigator.clipboard.writeText(generatedPassword).then(function () {
                button.html('<i class="fas fa-check"></i> Скопійовано')
                    .removeClass('btn-outline-info').addClass('btn-success');
                setTimeout(() => {
                    button.html('<i class="fas fa-copy"></i> Копіювати')
                        .removeClass('btn-success').addClass('btn-outline-info');
                }, 2000);
            }).catch(function () {
                $('#generatedPassword').select();
                document.execCommand('copy');
                button.html('<i class="fas fa-check"></i> Скопійовано')
                    .removeClass('btn-outline-info').addClass('btn-success');
                setTimeout(() => {
                    button.html('<i class="fas fa-copy"></i> Копіювати')
                        .removeClass('btn-success').addClass('btn-outline-info');
                }, 2000);
            });
        }
    });

    setTimeout(function () {
        $('#generatePassword').click();
    }, 100);

    $('form').on('submit', function (e) {
        const password = $('#newPassword').val();
        const confirmPassword = $('#confirmPassword').val();

        if (password !== confirmPassword) {
            e.preventDefault();
            alert('Паролі не співпадають!');
            return false;
        }

        const result = assessPasswordStrength(password);
        if (result.score < 3) {
            if (!confirm('Пароль досить слабкий. Ви впевнені, що хочете продовжити?')) {
                e.preventDefault();
                return false;
            }
        }

        $('#submitBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Обробка...');
    });
});

document.addEventListener('DOMContentLoaded', function () {
    const resendBtn = document.getElementById('resendBtn');
    const countdownEl = document.getElementById('countdown');

    // Start client-side cooldown if server provided remaining seconds
    let remaining = 0;
    if (countdownEl && countdownEl.textContent) {
        const m = countdownEl.textContent.match(/\((\d+)s\)/);
        if (m) remaining = parseInt(m[1], 10);
    }

    function startCooldown(sec) {
        let t = sec;
        if (resendBtn) resendBtn.disabled = true;
        const iv = setInterval(() => {
            if (t <= 0) {
                clearInterval(iv);
                if (resendBtn) resendBtn.disabled = false;
                if (countdownEl) countdownEl.textContent = '';
                return;
            }
            if (countdownEl) countdownEl.textContent = `(${t}s)`;
            t -= 1;
        }, 1000);
    }

    if (remaining > 0) startCooldown(remaining);

    // OTP inputs (6 separate fields) behavior
    const otpInputs = Array.from(document.querySelectorAll('.otp-input'));
    const hiddenCode = document.getElementById('code');
    const verifyForm = document.getElementById('verifyForm');
    const confirmBtn = document.getElementById('confirmBtn');

    if (otpInputs.length === 6) {
        otpInputs.forEach((input, idx) => {
            input.addEventListener('input', (e) => {
                const v = e.target.value.replace(/[^0-9]/g, '');
                e.target.value = v.slice(0, 1);
                if (v.length > 0 && idx < otpInputs.length - 1) {
                    otpInputs[idx + 1].focus();
                }
            });

            input.addEventListener('keydown', (e) => {
                if (e.key === 'Backspace' && !e.target.value && idx > 0) {
                    otpInputs[idx - 1].focus();
                }
            });

            input.addEventListener('paste', (e) => {
                e.preventDefault();
                const paste = (e.clipboardData || window.clipboardData).getData('text');
                const digits = paste.replace(/\D/g, '').slice(0, 6).split('');
                for (let i = 0; i < digits.length; i++) {
                    if (otpInputs[i]) otpInputs[i].value = digits[i];
                }
                // focus after paste
                const next = Math.min(digits.length, otpInputs.length - 1);
                otpInputs[next].focus();
            });
        });

        // On submit, combine into hidden field
        if (verifyForm) {
            verifyForm.addEventListener('submit', (e) => {
                const code = otpInputs.map(i => i.value || '').join('');
                if (hiddenCode) hiddenCode.value = code;
                // basic validation
                if (code.length !== 6) {
                    e.preventDefault();
                    alert('Insira o código de 6 dígitos.');
                    otpInputs[0].focus();
                    return false;
                }
                if (confirmBtn) confirmBtn.disabled = true;
            });
        }
    }

});

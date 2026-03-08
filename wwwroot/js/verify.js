document.addEventListener('DOMContentLoaded', function () {
    const resendBtn = document.getElementById('resendBtn');
    const countdownEl = document.getElementById('countdown');

    // Start client-side cooldown if server provided remaining seconds
    let remaining = 0;
    if (countdownEl && countdownEl.textContent) {
        const m = countdownEl.textContent.match(/\((\d+)s\)/);
        if (m) remaining = parseInt(m[1], 10);
    }

    // Resend via AJAX
    const resendForm = document.getElementById('resendForm');
    if (resendForm) {
        resendForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            // get antiforgery token
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            const token = tokenInput ? tokenInput.value : null;
            if (!token) {
                if (verifyMessage) verifyMessage.textContent = 'Erro interno: token antifalsificação em falta.';
                return;
            }

            if (resendBtn) resendBtn.disabled = true;

            try {
                const res = await fetch('/Account/ResendAjax', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    }
                });
                const data = await res.json();
                if (data.success) {
                    if (verifyMessage) verifyMessage.textContent = data.message || 'Código reenviado.';
                    startCooldown(data.remaining || 60);
                } else if (data.needsLogin && data.loginUrl) {
                    window.location.href = data.loginUrl;
                    return;
                } else {
                    if (verifyMessage) verifyMessage.textContent = data.message || 'Erro ao reenviar.';
                    if (data.remaining) startCooldown(data.remaining);
                }
            } catch (err) {
                if (verifyMessage) verifyMessage.textContent = 'Erro de rede ao reenviar.';
            } finally {
                // button state handled by cooldown
            }
        });
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

    // message element for aria-live
    const verifyMessage = document.getElementById('verifyMessage');

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

        // On submit, combine into hidden field and submit via AJAX
        if (verifyForm) { 
            verifyForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                const code = otpInputs.map(i => i.value || '').join('');
                if (hiddenCode) hiddenCode.value = code;
                // basic validation
                if (code.length !== 6) {
                    if (verifyMessage) verifyMessage.textContent = 'Insira o código de 6 dígitos.';
                    otpInputs[0].focus();
                    return;
                }

                // get antiforgery token
                const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                const token = tokenInput ? tokenInput.value : null;

                if (confirmBtn) {
                    confirmBtn.disabled = true;
                    document.getElementById('confirmBtnText').textContent = 'A verificar...';
                    document.getElementById('confirmSpinner').classList.remove('d-none');
                }

                if (!token) { 
                    if (verifyMessage) verifyMessage.textContent = 'Erro interno: token antifalsificação em falta.';
                    resetConfirmBtn();
                    return;
                }

                try {
                    const res = await fetch('/Account/VerifyAjax', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': token
                        },
                        body: JSON.stringify({ code })
                    });

                    const data = await res.json();
                    if (data.success) {
                        // redirect if provided
                        if (data.redirectUrl) {
                            window.location.href = data.redirectUrl;
                            return;
                        }
                        if (verifyMessage) verifyMessage.textContent = data.message || 'Verificado com sucesso.';
                    } else {
                        if (data.needsLogin && data.loginUrl) {
                            window.location.href = data.loginUrl;
                            return;
                        }
                        if (verifyMessage) verifyMessage.textContent = data.message || 'Erro na verificação.';
                    }
                } catch (err) {
                    if (verifyMessage) verifyMessage.textContent = 'Erro de rede. Tente novamente.'; 
                } finally {
                    resetConfirmBtn();
                }
            });
        }

        function resetConfirmBtn() {
            if (confirmBtn) {
                confirmBtn.disabled = false;
                document.getElementById('confirmBtnText').textContent = 'Confirmar';
                document.getElementById('confirmSpinner').classList.add('d-none');
            }
        }
    }

});

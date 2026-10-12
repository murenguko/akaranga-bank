// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Lock POST forms after the first accepted submission to prevent accidental
// double clicks from repeating a financial or administrative operation.
document.addEventListener("submit", (event) => {
    const form = event.target;

    if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== "post") {
        return;
    }

    // Inline confirmations (for example, card opposition) may cancel submit.
    if (event.defaultPrevented) {
        return;
    }

    if (form.dataset.submitPending === "true") {
        event.preventDefault();
        return;
    }

    form.dataset.submitPending = "true";
    form.setAttribute("aria-busy", "true");

    const submitButtons = form.querySelectorAll('button[type="submit"], input[type="submit"], button:not([type])');
    submitButtons.forEach((button) => {
        if (button.disabled) {
            return;
        }

        button.dataset.submitLocked = "true";
        button.disabled = true;

        if (button instanceof HTMLButtonElement) {
            button.dataset.submitOriginalHtml = button.innerHTML;
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Traitement en cours…';
        } else {
            button.dataset.submitOriginalValue = button.value;
            button.value = "Traitement en cours…";
        }
    });
});

// Browsers can restore a submitted page from the back-forward cache.
window.addEventListener("pageshow", () => {
    document.querySelectorAll('[data-submit-locked="true"]').forEach((button) => {
        button.disabled = false;
        delete button.dataset.submitLocked;

        if (button instanceof HTMLButtonElement && button.dataset.submitOriginalHtml !== undefined) {
            button.innerHTML = button.dataset.submitOriginalHtml;
            delete button.dataset.submitOriginalHtml;
        } else if (button instanceof HTMLInputElement && button.dataset.submitOriginalValue !== undefined) {
            button.value = button.dataset.submitOriginalValue;
            delete button.dataset.submitOriginalValue;
        }
    });

    document.querySelectorAll('form[data-submit-pending="true"]').forEach((form) => {
        delete form.dataset.submitPending;
        form.removeAttribute("aria-busy");
    });
});

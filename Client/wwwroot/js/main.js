window.scrollToBottomById = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.focusById = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.focus();
    }
};
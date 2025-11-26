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

// Citation click handler
let dotNetRef = null;

window.registerCitationClickHandler = (objRef) => {
    dotNetRef = objRef;

    // Use event delegation to handle clicks on dynamically created elements
    document.addEventListener('click', (e) => {
        const viewSourceLink = e.target.closest('.view-source-link');
        if (viewSourceLink && dotNetRef) {
            e.preventDefault();
            const pageNumber = parseInt(viewSourceLink.dataset.page);
            const citationIndex = parseInt(viewSourceLink.dataset.citationIndex);
            const messageId = viewSourceLink.dataset.messageId;
            dotNetRef.invokeMethodAsync('OnViewSourceClick', pageNumber, citationIndex, messageId);
        }
    });
};
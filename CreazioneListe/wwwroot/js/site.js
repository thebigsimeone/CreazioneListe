// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function scaricaTutti() {
    const links = document.querySelectorAll(".download-link");
    const delay = 1000; // Ritardo tra i download (in millisecondi)

    links.forEach((link, index) => {
        setTimeout(() => {
            console.log(`Scaricando il file: ${link.getAttribute("data-file-name")}`);
            link.click();
        }, index * delay);
    });
}

document.querySelectorAll('.send-file-form').forEach(form => {
    form.addEventListener('submit', async event => {
        event.preventDefault();
        const formData = new FormData(event.target);

        try {
            const response = await fetch(event.target.action, {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                alert("File inviato con successo!");
            } else {
                const error = await response.text();
                alert(`Errore: ${error}`);
            }
        } catch (error) {
            alert(`Errore di connessione: ${error.message}`);
        }
    });
});
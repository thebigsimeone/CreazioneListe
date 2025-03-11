// Scarica tutti i file
function scaricaTutti() {
    const links = document.querySelectorAll("td a.btn-outline-primary");
    const delay = 750;

    if (links.length === 0) {
        console.warn("Nessun link di download trovato.");
        return;
    }

    links.forEach((link, index) => {
        setTimeout(() => {
            const url = link.href;
            if (!url) {
                console.warn("URL di download non trovato per un file.");
                return;
            }

            const fileName = link.getAttribute("download") || "download";
            console.log(`Scaricando il file: ${fileName}`);

            const anchor = document.createElement("a");
            anchor.href = url;
            anchor.download = fileName;
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
        }, index * delay);
    });
}

// Invia file ai fornitori con spinner e stato
document.querySelectorAll('.send-file-form').forEach(form => {
    form.addEventListener('submit', async event => {
        event.preventDefault();

        const formData = new FormData(event.target);
        const fileName = form.dataset.fileName;
        const statusContainer = document.querySelector(`.status-container[data-file-name="${fileName}"]`);
        const spinner = statusContainer.querySelector('.spinner-border');
        const statusMessage = statusContainer.querySelector('.status-message');

        // Reset stato
        statusMessage.textContent = '';
        spinner.classList.remove('d-none');

        try {
            const response = await fetch(event.target.action, {
                method: 'POST',
                body: formData
            });

            spinner.classList.add('d-none');

            if (response.ok) {
                statusMessage.textContent = "Inviato con successo!";
                statusMessage.classList.remove('text-danger');
                statusMessage.classList.add('text-success');
            } else {
                const errorText = await response.text();
                statusMessage.textContent = `Errore: ${errorText}`;
                statusMessage.classList.remove('text-success');
                statusMessage.classList.add('text-danger');
            }
        } catch (error) {
            spinner.classList.add('d-none');
            statusMessage.textContent = `Errore di connessione: ${error.message}`;
            statusMessage.classList.remove('text-success');
            statusMessage.classList.add('text-danger');
        }
    });
});

// Gestione invio form per selezione
document.addEventListener('DOMContentLoaded', function () {
    const selezionaForm = document.getElementById("selezionaForm");
    if (selezionaForm) {
        selezionaForm.addEventListener("submit", function (event) {
            const checkboxes = document.querySelectorAll('.row-checkbox');
            const form = event.target;

            console.log("Invio Form...");

            // Rimuove eventuali campi input aggiunti precedentemente
            document.querySelectorAll('.formato-dinamico, .unisci-dinamico').forEach(el => el.remove());

            checkboxes.forEach((checkbox, index) => {
                if (checkbox.checked) {
                    // Formato
                    const formatoInput = document.createElement("input");
                    formatoInput.type = "hidden";
                    formatoInput.name = "formato[]";
                    formatoInput.value = document.getElementById(`formato_${index}`).value;
                    formatoInput.classList.add('formato-dinamico');
                    form.appendChild(formatoInput);

                    // Unisci
                    const unisciInput = document.createElement("input");
                    unisciInput.type = "hidden";
                    unisciInput.name = "unisci[]";
                    unisciInput.value = document.getElementById(`unisci_${index}`).checked ? "true" : "false";
                    unisciInput.classList.add('unisci-dinamico');
                    form.appendChild(unisciInput);
                }
            });

            console.log("Dati inviati:", new FormData(selezionaForm));
        });
    }
});

// Attiva/disattiva formato e unisci
function toggleFormato(index) {
    let checkbox = document.getElementById(`rowCheck_${index}`);
    let formatoInput = document.getElementById(`formato_${index}`);
    let unisciCheckbox = document.getElementById(`unisci_${index}`);

    formatoInput.disabled = !checkbox.checked;
    unisciCheckbox.disabled = !checkbox.checked;

    if (!checkbox.checked) {
        unisciCheckbox.checked = false;
        unisciCheckbox.value = "false";
    }
}

// Cambia valore unisci
function toggleUnisci(index) {
    let checkbox = document.getElementById(`unisci_${index}`);
    checkbox.value = checkbox.checked ? "true" : "false";
}

// Aggiorna valore formato
function aggiornaFormato(index, formato) {
    let formatoInput = document.getElementById(`formato_${index}`);
    formatoInput.value = formato;
}

// Seleziona/Deseleziona tutte le righe
let tutteSelezionate = false;
function selezionaTutte() {
    const checkboxes = document.querySelectorAll('.row-checkbox');
    tutteSelezionate = !tutteSelezionate;

    checkboxes.forEach(checkbox => {
        checkbox.checked = tutteSelezionate;
        toggleFormato(checkbox.getAttribute('id').split('_')[1]);
    });

    const button = document.querySelector('button[onclick="selezionaTutte()"]');
    if (button) {
        button.textContent = tutteSelezionate ? "Deseleziona Tutto" : "Seleziona Tutto";
    }
}

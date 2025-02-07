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

function aggiornaFormato(index, formato) {
    let formatoInput = document.getElementById(`formato_${index}`);
    formatoInput.value = formato;
}

function toggleFormato(index) {
    let checkbox = document.getElementById(`rowCheck_${index}`);
    let formatoInput = document.getElementById(`formato_${index}`);

    if (checkbox.checked) {
        formatoInput.disabled = false; // Abilita l'invio del formato solo se la riga è selezionata
    } else {
        formatoInput.disabled = true; // Disabilita il formato se la riga non è selezionata
        formatoInput.value = "B"; // Reset al valore di default
    }
}

let tutteSelezionate = false;

function selezionaTutte() {
    const checkboxes = document.querySelectorAll('.row-checkbox');
    tutteSelezionate = !tutteSelezionate; // Cambia lo stato
    checkboxes.forEach(checkbox => {
        checkbox.checked = tutteSelezionate;
        toggleFormato(checkbox.getAttribute('id').split('_')[1]); // Aggiorna il formato
    });

    // Modifica il testo del pulsante in base allo stato corrente
    const button = document.querySelector('button[onclick="selezionaTutte()"]');
    if (button) {
        button.textContent = tutteSelezionate ? "Deseleziona Tutto" : "Seleziona Tutto";
    }
}

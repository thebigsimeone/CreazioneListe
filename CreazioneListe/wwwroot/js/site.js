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
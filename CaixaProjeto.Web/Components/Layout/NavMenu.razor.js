// No telemóvel: depois de escolher uma página no menu, fecha o menu
const alternar = document.getElementById("menu-alternar");
const menu = document.getElementById("menu-principal");

if (alternar && menu) {
    menu.addEventListener("click", function (evento) {
        if (evento.target.closest("a")) {
            alternar.checked = false;
        }
    });
}

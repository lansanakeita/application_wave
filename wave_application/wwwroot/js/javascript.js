"use strict";

/************************* Traitement assemblage ********************/
const ofAssemblage = document.getElementById("ofAssemblage");
if (ofAssemblage) {
    ofAssemblage.focus();
}


/************************* Traitement Injection ********************/
const ofInjection = document.getElementById("of");
if (ofInjection) {
    ofInjection.focus();
}

/************************* Traitement Verfication24H ********************/
const of24 = document.getElementById("of24H");
if (of24) {
    of24.focus();
}



/******************************SOUMISSION DES BTN******************/

function showLoadingIndicator() {
    var loadingIndicator = document.getElementById("loadingIndicator");
    loadingIndicator.style.display = "block";
}

// When the loading indicator is hidden, hide the button form
function hideButtonForm() {
    var buttonForm = document.getElementById("buttonForm");
    buttonForm.style.display = "none";
}

// Click handler for the buttons
var buttons = document.querySelectorAll("button[type='submit']");
for (var i = 0; i < buttons.length; i++) {
    buttons[i].addEventListener("click", showLoadingIndicator);
    buttons[i].addEventListener("click", hideButtonForm);
}
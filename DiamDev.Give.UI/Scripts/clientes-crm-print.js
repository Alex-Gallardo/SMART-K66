(function () {
    'use strict';
    var button = document.getElementById('crmPrintButton');
    if (button) button.addEventListener('click', function () { window.print(); });
}());

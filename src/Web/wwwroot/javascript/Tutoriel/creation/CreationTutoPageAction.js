﻿'use strict';
$(document).ready(function () {
    // Création du tuto.
    let id;
    $('[data-creationTuto]').on('click', event => {
        event.preventDefault();
        let curTarget = event.currentTarget;

        var href = window.location.pathname + "?handler=CreeTutorielDetails";

        $.ajax({
            type: 'POST',
            url: href,
            data: new FormData(curTarget.form),
            headers: {
                RequestVerificationToken:
                    $('input:hidden[name="__RequestVerificationToken"]').val()
            },
            cache: false,
            contentType: false,
            processData: false,
            success: function (data) {
                creationReussie(data, curTarget);
            },
            error: function () {
                window.scriptToastNotification.AjouterNotification("Le tutoriel n'a pas été créé!", false);
            }
        });
    });

    function creationReussie(data) {
        $('#imgBanierre').attr('src', data.value.imgUrl);
        id = data.id;
        const state = { 'id': data.id, 'handler': 'CreeTutorielDetails' };
        const url = '/Tutoriel/CreationTuto';

        history.pushState(state, '', url);

        $('[name="idTutoP"]').val(data.value.idTutoP);
        $('[id="idTutoP"]').val(data.value.idTutoP);
        $('[id="idTutoP2"]').val(data.value.idTutoP);
        $('[name="idtutoVal"]').val(data.value.idTutoP);
        console.log(data.value.idTutoP)
        $('#btnAddRangee').removeClass('disabled');
        $('<button id="btnModifierTuto" data-modificationTuto type="submit" class="btn btn-outline-primary">Enregistré modification</button>').insertAfter($('#btnCreeTuto'));
        $('#btnCreeTuto').remove();
        // lancement du toast
        window.scriptToastNotification.AjouterNotification('Le tutoriel a été créé!', true);
    }
});
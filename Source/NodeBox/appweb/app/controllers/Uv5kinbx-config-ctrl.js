angular.module("Uv5kinbx")
    .controller("uv5kiConfigCtrl", function ($scope, $interval, $serv, $lserv) {

        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 6;
        ctrl.editor = null;
        ctrl.editor_hash = null;
        ctrl.cambios = false;

        /** */
        ctrl.lc = {};
        ctrl.lcext = {};

        /** */
        //** */
        ctrl.change_pagina = function (new_pagina) {
            /** Cargamos la nueva lista */
            ctrl.pagina = new_pagina;
        };

        /** */
        ctrl.SalvarCambios = function () {
            var data_rem = { fichero: ctrl.editor.getValue() };
            if ($lserv.validate(5, data_rem.fichero) == false) {
                return;
            }
            ctrl.lcext = data_rem;
            alertify.confirm($lserv.translate("Desea Salvar los cambios efectuados?"), function () {
                lconfig_save();
            }, function () {
                alertify.message($lserv.translate("Operacion Cancelada"));
            });
        };

        /** */
        ctrl.autosave = function () {
            // body...
        };

        //** */
        ctrl.validate = function (par) {
            return $lserv.validate(par.validar, par.valor, par.margenes.max, par.margenes.min);
        };

        //** Obtiene la lista seg�n la p�gina*/
        function lp_get() {
            switch (ctrl.pagina) {
                case 0:
                    return ctrl.lc.pgn;
                case 1:
                    return ctrl.lc.pif;
                case 2:
                    return ctrl.lc.prd;
                case 3:
                    return ctrl.lc.pcf;
                case 4:
                    return ctrl.lc.pit;
                case 5:
                    return ctrl.lc.ppx;
                default:
                    return { name: "Error Pagina", par: [] };
            }
        }

        //** Salva la lista segun la pagina */
        function lp_set(new_data) {
            switch (ctrl.pagina) {
                case 0:
                    ctrl.lc.pgn = new_data;
                    break;
                case 1:
                    ctrl.lc.pif = new_data;
                    break;
                case 2:
                    ctrl.lc.prd = new_data;
                    break;
                case 3:
                    ctrl.lc.pcf = new_data;
                    break;
                case 4:
                    ctrl.lc.pit = new_data;
                    break;
                case 5:
                    ctrl.lc.ppx = new_data;
                    break;
                default:
                    break;
            }
        }

        /** */
        function lconfig_load() {
            $serv.lconfig_ext_get().then(function (response) {
                ctrl.lcext = response.data;
                ctrl.editor_hash = CryptoJS.MD5(ctrl.lcext.fichero);
                $lserv.validate(5, ctrl.lcext.fichero);
                ctrl.editor.setValue(ctrl.lcext.fichero, -1);

            }, function (response) {
                alertify.error($lserv.translate("No se ha podido ejecutar la operacion."));
            });
        }

        //** */
        function lconfig_save(data) {
            $serv.lconfig_ext_set(ctrl.lcext).then(function (response) {
                alertify.success($lserv.translate("Operacion Ejecutada."));
                ctrl.editor_hash = CryptoJS.MD5(ctrl.editor.getValue());
            }, function (response) {
                alertify.error($lserv.translate("No se ha podido ejecutar la operacion."));
            });
        }

        /** */
        $scope.$on('$viewContentLoaded', function () {
            // $(".editor").jqte();
            ctrl.editor = ace.edit("ace-editor");
            var XmlMode = ace.require("ace/mode/xml").Mode;
            ctrl.editor.session.setMode(new XmlMode());

            // editor.setValue("the new text here"); // or session.setValue
            // editor.getValue(); // or session.getValue
            ctrl.editor.resize();

            ctrl.editor.on("change", function (e) {
                if (!(ctrl.editor.curOp && ctrl.editor.curOp.command.name)) {
                    ctrl.cambios = false;
                }
                else {
                    ctrl.cambios = true;
                }
            });

            lconfig_load();

        });
        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            var editor_hash = CryptoJS.MD5(ctrl.editor.getValue());
            if (editor_hash.toString() != ctrl.editor_hash.toString()) {
                ctrl.SalvarCambios();
            }
            ctrl.editor.destroy();
            ctrl.editor.container.remove();
        });

    });

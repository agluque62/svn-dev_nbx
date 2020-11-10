/** */
angular.module("Uv5kinbx")
    .controller("uv5kiGeneralesCtrl", function ($scope, $interval, $serv, $lserv) {
        /** Inicializacion */
        var ctrl = this;

        ctrl.pagina = 0;

        /** Estados.. */
        ctrl.std = {}; /* 0 = Disabled. 1 = Enabled. */
        $lserv.globalType(srvtypes.None);

        //ctrl.std.type = srvtypes.None;

        load_std();

        /** Lista de Preconfiguraciones */
        ctrl.preconf = [];
        load_preconf();

        /** Servicios del Controlador */
        /** Servicios Pagina de Estado*/
        //** */
        ctrl.txtModo = function (std) {
            //return std == stdcodes.Error ? $lserv.translate("Parado") :
            //    std == stdcodes.Esclavo ?  $lserv.translate("Esclavo") :
            //    std == stdcodes.Maestro ?  $lserv.translate("Maestro") : $lserv.translate("Estado Erroneo");
            return std === undefined ? "" : std.level;
        };

        //** */
        ctrl.colorEstado = function (std) {
            //return std == stdcodes.Error ? "danger" :
            //    std == stdcodes.Esclavo ? "info" :
            //    std == stdcodes.Maestro ? "success" : "danger";

            return std === undefined || std.std === states.Disabled || std.level === levels.Error ? "danger" :
                std.level === levels.Slave && std.std === states.Running ? "info" :
                    std.level === levels.Master && std.std === states.Running ? "success" : "danger";
        };

        ctrl.RowBg = function (std) {
            return std === undefined || std.std === states.Disabled || std.level === levels.Error ? "row bg-danger" :
                std.level === levels.Slave && std.std === states.Running ? "row bg-warning" :
                    std.level === levels.Master && std.std === states.Running ? "row bg-white" : "row bg-danger";

        };

        //** */
        ctrl.txtEstado = function (std) {
            //return std == stdcodes.Error ? $lserv.translate("Parado") :
            //    std == stdcodes.Esclavo ?  $lserv.translate("En Espera") :
            //    std == stdcodes.Maestro ?  $lserv.translate("Activado") : $lserv.translate("Estado Erroneo");
            return std === undefined ? "" : std.std;
        };

        //** */
        ctrl.txtEstadoMn = function (std) {
            //return std == "ERROR" ? $lserv.translate("Parado") :
            //    std == "Disabled" ? $lserv.translate("En Espera") :
            //    std == "Stopped" ? $lserv.translate("Parado") :
            //    std == "Running" ? $lserv.translate("Activado") : $lserv.translate("Estado Erroneo");
            return std === undefined ? "" : std.std;
        };

        ctrl.DisableRadioServiceComponent = function () {
            //var type = $lserv.globalType();
            //return (type != srvtypes.Radio && type != srvtypes.Mixed);
            return true;
        };

        ctrl.DisablePhoneServiceComponent = function (isfocus) {
            var type = $lserv.globalType();
            if (isfocus)
                return type != srvtypes.Phone;
            return (type != srvtypes.Phone && type != srvtypes.Mixed);
        };

        /** Servicios Pagina de Preconfiguraciones*/
        //** */
        ctrl.eliminar_pre = function (pre) {
            var pregunta = pre.nombre + ". " + $lserv.translate("¿Quiere eliminar la Preconfiguracion?");
            alertify.confirm(pregunta,
                function () {
                    $("body").css("cursor", "progress");
                    $serv.preconf_delete(pre.nombre).then(
                        function () {
                            $("body").css("cursor", "default");
                            alertify.success($lserv.translate("Operacion Ejecutada."));
                            load_preconf();
                        },
                        function () {
                            $("body").css("cursor", "default");
                            alertify.error($lserv.translate("No se ha podido ejecutar la operacion."));
                        });
                },
                function () {
                    alertify.message($lserv.translate("Operacion Cancelada"));
                });
        };

        //** */
        ctrl.activar_pre = function (pre) {
            var pregunta = pre.nombre + ". " + $lserv.translate("¿Quiere activar esta Preconfiguracion?");
            alertify.confirm(pregunta,
                function () {
                    $("body").css("cursor", "progress");
                    $serv.preconf_activate(pre.fecha, pre.nombre).then(
                        function () {
                            $("body").css("cursor", "default");
                            alertify.success($lserv.translate("Operacion Ejecutada."));
                            load_preconf();
                        },
                        function () {
                            $("body").css("cursor", "default");
                            alertify.error($lserv.translate("No se ha podido ejecutar la operacion."));
                        });
                },
                function () {
                    alertify.message($lserv.translate("Operacion Cancelada"));
                });
        };

        /** */
        ctrl.salvar_como = function () {

            if (ctrl.preconf.length >= maxPreconf) {
                alertify.alert($lserv.translate("Se ha alcanzado el número máximo de preconfiguraciones que se pueden almacenar. Elimine alguna antes de generar una nueva."));
                return;
            }
            alertify.prompt($lserv.translate('Introduzca un identificador'), ''
                , function (evt, value) {
                    alertify.success('You entered: ' + value);
                    if (!value) {
                        alertify.error('No Value');
                        return;
                    }
                    for (var i = 0; i < ctrl.preconf.length; i++) {
                        if (value == ctrl.preconf[i].nombre) {
                            alertify.alert(value.toString() + '. ' + $lserv.translate("Ya existe una preconfiguración con este nombre. Debe introducir un nombre no utilizado."));
                            return;
                        }
                    }
                    var pregunta = value + ". " + $lserv.translate("¿Quiere salvar la configuracion actual con este nombre?");
                    alertify.confirm(pregunta
                        , function () {
                            $serv.preconf_saveas("", value).then(
                                function (response) {
                                    //alert("Preconfiguración "  + name + " guardada correctamente.");
                                    alertify.success($lserv.translate("Operacion Ejecutada."));
                                    load_preconf();
                                }
                                , function (response) {
                                    console.log(response);
                                    alertify.error($lserv.translate("No se ha podido ejecutar la operacion."));
                                });
                        }
                        , function () {
                            alertify.message($lserv.translate("Operacion Cancelada"));
                        });

                }
                , function () {
                    alertify.message($lserv.translate("Operacion Cancelada"));
                }
            );
        };

        /** */
        ctrl.logs = function () {
            var win = window.open('logs/logfile.csv', '_blank');
            win.focus();
        };

        /** Arrancar la visualizacion de versiones. */
        ctrl.versiones = null;
        ctrl.VersionDetailShow = function () {
            $serv.versiones_get().then(function (response) {
                ctrl.versiones = response.data;
                $("#VersionDetail").modal("show");
            }
                , function (response) {
                    alertify.error($lserv.translate("No se ha podido ejecutar la operacion."));
                });
        };

        ctrl.versiones_export = function () {
            var csvData = "Modulo;" +
                "Version;" +
                "Componente;" +
                "Fichero;" +
                "Tamano;" +
                "Fecha;" +
                "Hash\r\n";
            $.each(ctrl.versiones.Components, function (index, comp) {
                $.each(comp.Files, function (index1, file) {
                    var item = "Nodebox;" +
                        (ctrl.versiones.Version + ";") +
                        (comp.Name + ";") +
                        (file.Path + ";") + (file.Size + ";") + (file.Date + ";") + (file.MD5 + "\r\n");
                    csvData += item;
                });
            });
            /*
            var myLink = document.createElement('a');
            myLink.download = 'nbx_versiones.csv';
            myLink.href = "data:application/csv," + escape(csvData);
            myLink.click();
            */
            var blob = new Blob([csvData], { type: "text/plain;charset=utf-8" });
            saveAs(blob, "nbx_versiones.csv");
        };

        /** */
        function load_std() {
            /* Obtener el estado del servidor... */
            $serv.stdgen_get().then(function (response) {
                ctrl.std = response.data;
                $lserv.GlobalStd(ctrl.std);
                $lserv.globalType(ctrl.std.type);
                $lserv.RdModulesInfoSet(ctrl.std.rad.modules);
                //$lserv.RdModuleExist("1+1");
                //$lserv.RdModuleExist("M+N");
                //$lserv.RdModuleExist("HF");
            }, function (response) {
                console.log(response);
            });
        }

        /** */
        function load_preconf() {
            /* Obtener el estado del servidor... */
            $serv.preconf_list().then(function (response) {
                ctrl.preconf = response.data;
                //console.log(ctrl.preconf);
            }
                , function (response) {
                    console.log(response);
                });
        }

        /** Funcion Periodica del controlador */
        var timer = $interval(function () {

            load_std();
            load_preconf();

        }, pollingTime);

        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
        });

    });



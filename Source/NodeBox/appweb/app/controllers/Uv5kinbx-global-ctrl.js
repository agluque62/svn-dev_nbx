/** */
angular.module("Uv5kinbx")
    .controller("uv5kiGlobalCtrl", function ($scope, $interval, $location, $translate, $serv, $lserv) {
        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 0;
        /** Lista de Incidencias */
        ctrl.listainci = [];
        ctrl.HashCode = 0;
        ctrl.timer = 0;
        ctrl.title = "";

        getInci();


        ctrl.user = "agl1";
        ctrl.date = (new Date()).toLocaleDateString();
        ctrl.hora = (new Date()).toLocaleTimeString();
        $location.path("/");


        /** */
        ctrl.appver = app_version;

        //** */
        ctrl.decodeHtml = function (html) {
            var txt = document.createElement("textarea");
            txt.innerHTML = html;
            return txt.value;
        };

        ctrl.RadioOptionsShow = function () {
            var type = $lserv.globalType();
            return type === srvtypes.Radio || type === srvtypes.Mixed;
        };

        ctrl.PhoneOptionsShow = function () {
            var type = $lserv.globalType();
            return ctrl.appver >= 1 && (type === srvtypes.Phone || type === srvtypes.Mixed);
        };

        // Paginado Incidencias
        ctrl.inciPageSize = 4;
        ctrl.inciCurrentPage = 1;
        ctrl.inciPage = [];
        ctrl.inciPages = 0;
        function inciPaginate() {
            var begin = ((ctrl.inciCurrentPage - 1) * ctrl.inciPageSize),
                end = begin + ctrl.inciPageSize;
            ctrl.inciPage = ctrl.listainci.slice(begin, end);
            var item1 = Math.floor(ctrl.listainci.length / ctrl.inciPageSize);
            var item2 = (ctrl.listainci.length % ctrl.inciPageSize) !== 0 ? 1 : 0;
            ctrl.inciPages = ctrl.listainci.length === 0 ? 1 : item1 + item2;
        }
        $scope.$watch('ctrl.inciCurrentPage', function () {

            console.log(ctrl.inciCurrentPage);
            inciPaginate();
        });

        ctrl.logs = function () {
            var win = window.open('/logs', '_blank');
            win.focus();
        };
        /** Funciones o servicios */
        function getInci() {
            $serv.inci_get().then(function (response) {
                if (response.status == 200 && (typeof response) == 'object') {
                    if (ctrl.HashCode != response.data.hash) {
                        ctrl.listainci = response.data.li;
                        ctrl.HashCode = response.data.hash;
                        inciPaginate();
                    }
                    // console.log(ctrl.listainci);
                    /** */
                    if (userLang != response.data.lang) {
                        userLang = response.data.lang;
                        if (userLang.indexOf("en") == 0)
                            $translate.use('en_US');
                        else if (userLang.indexOf("fr") == 0)
                            $translate.use('fr_FR');
                        else
                            $translate.use('es_ES');
                    }
                }
                else {
                    /** El servidor me devuelve errores... */
                    // window.open(routeForDisconnect, "_self");
                }
            }
                , function (response) {
                    // Error. No puedo conectarme al servidor.
                    // window.open(routeForDisconnect, "_self");
                });
        }

        function getTitle() {
            var type = $lserv.globalType();
            return type === srvtypes.Radio ? "ULISES V 5000 I. Radio Server" :
                type === srvtypes.Phone ? "ULISES V 5000 I. Phone Server" : "ULISES V 5000 I. Nodebox";
        }

        /** Funcion Periodica del controlador */
        var timer = $interval(function () {

            ctrl.date = moment().format('ll');
            ctrl.hora = moment().format('LTS');

            ctrl.timer++;

            if ((ctrl.timer % 5) == 0) {
                getInci();
            }

            ctrl.title = getTitle();

        }, 1000);

        /** */
        $scope.$on('$viewContentLoaded', function () {
            /** Alertify */
            alertify.defaults.transition = 'zoom';
            alertify.defaults.glossary = {
                title: $lserv.translate("ULISES V 5000 I. Nodebox"),
                ok: $lserv.translate("Aceptar"),
                cancel: $lserv.translate("Cancelar")
            };

        });

        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
        });

    });



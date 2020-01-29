/** Variables Globales */
var Simulate = location.port == 1444;
var pollingTime = 5000;
var maxPreconf = Simulate==true ? 16 : 8;
var app_version = 2;            /** 0: Version 2.5.3 o anterior. 1: Version 2.5.4 o posterior. 2: Version 2.5.6 o posterior */
var userLang = navigator.language;

/** */
var Uv5kinbx = angular.module('Uv5kinbx', ['ngRoute', 'ui.bootstrap', 'pascalprecht.translate']);

/** */
Uv5kinbx.config(function ($translateProvider) {
    // Our translations will go in here
    $translateProvider.useStaticFilesLoader({
        prefix: '/languages/',
        suffix: '.json'
    });

    if (userLang.indexOf("en") == 0)
        $translateProvider.use('en_US');
    else if (userLang.indexOf("fr") == 0)
        $translateProvider.use('fr_FR');
    else
        $translateProvider.use('es_ES');

    /** Configuraicion de las fechas a traves de momment */
    /** Español */
    moment.locale('es', {
        months : 'enero_febrero_marzo_abril_mayo_junio_julio_agosto_septiempbre_octubre_noviembre_diciembre'.split('_'),
        monthsShort : 'ene._feb._mar._abr._may._jun._jul._ago._sep._oct._nov._dic.'.split('_'),
        monthsParseExact : true,
        weekdays : 'domingo_lunes_martes_miercoles_jueves_viernes_sabado'.split('_'),
        weekdaysShort : 'dom._lun._mar._mie._jue._vie._sab.'.split('_'),
        weekdaysMin : 'Do_Lu_Ma_Mi_Ju_Vi_Sa'.split('_'),
        weekdaysParseExact : true,
        longDateFormat : {
            LT : 'HH:mm',
            LTS : 'HH:mm:ss',
            L : 'DD/MM/YYYY',
            LL : 'D MMMM YYYY',
            LLL : 'D MMMM YYYY HH:mm',
            LLLL : 'dddd D MMMM YYYY HH:mm'
        },
        calendar : {
            sameDay : '[Hoy a las] LT',
            nextDay : '[Mañana a las] LT',
            nextWeek : 'dddd [a las] LT',
            lastDay : '[Ayer a las] LT',
            lastWeek : 'dddd [anterior a las] LT',
            sameElse : 'L'
        },
        relativeTime : {
            future : 'en %s',
            past : 'hace %s',
            s : 'algunos segundos',
            m : 'un minuto',
            mm : '%d minutos',
            h : 'una hora',
            hh : '%d horas',
            d : 'un dia',
            dd : '%d dias',
            M : 'un mes',
            MM : '%d meses',
            y : 'un año',
            yy : '%d años'
        },
        dayOfMonthOrdinalParse : /\d{1,2}(er|e)/,
        ordinal : function (number) {
            return number + (number === 1 ? 'er' : 'e');
        },
        meridiemParse : /PM|AM/,
        isPM : function (input) {
            return input.charAt(0) === 'M';
        },
        // In case the meridiem units are not separated around 12, then implement
        // this function (look at locale/id.js for an example).
        // meridiemHour : function (hour, meridiem) {
        //     return /* 0-23 hour, given meridiem token and hour 1-12 */ ;
        // },
        meridiem : function (hours, minutes, isLower) {
            return hours < 12 ? 'PM' : 'AM';
        },
        week : {
            dow : 1, // Monday is the first day of the week.
            doy : 4  // The week that contains Jan 4th is the first week of the year.
        }
    });
    /** Francés */
    moment.locale('fr', {
        months : 'janvier_février_mars_avril_mai_juin_juillet_août_septembre_octobre_novembre_décembre'.split('_'),
        monthsShort : 'janv._févr._mars_avr._mai_juin_juil._août_sept._oct._nov._déc.'.split('_'),
        monthsParseExact : true,
        weekdays : 'dimanche_lundi_mardi_mercredi_jeudi_vendredi_samedi'.split('_'),
        weekdaysShort : 'dim._lun._mar._mer._jeu._ven._sam.'.split('_'),
        weekdaysMin : 'Di_Lu_Ma_Me_Je_Ve_Sa'.split('_'),
        weekdaysParseExact : true,
        longDateFormat : {
            LT : 'HH:mm',
            LTS : 'HH:mm:ss',
            L : 'DD/MM/YYYY',
            LL : 'D MMMM YYYY',
            LLL : 'D MMMM YYYY HH:mm',
            LLLL : 'dddd D MMMM YYYY HH:mm'
        },
        calendar : {
            sameDay : '[Aujourd’hui à] LT',
            nextDay : '[Demain à] LT',
            nextWeek : 'dddd [à] LT',
            lastDay : '[Hier à] LT',
            lastWeek : 'dddd [dernier à] LT',
            sameElse : 'L'
        },
        relativeTime : {
            future : 'dans %s',
            past : 'il y a %s',
            s : 'quelques secondes',
            m : 'une minute',
            mm : '%d minutes',
            h : 'une heure',
            hh : '%d heures',
            d : 'un jour',
            dd : '%d jours',
            M : 'un mois',
            MM : '%d mois',
            y : 'un an',
            yy : '%d ans'
        },
        dayOfMonthOrdinalParse : /\d{1,2}(er|e)/,
        ordinal : function (number) {
            return number + (number === 1 ? 'er' : 'e');
        },
        meridiemParse : /PD|MD/,
        isPM : function (input) {
            return input.charAt(0) === 'M';
        },
        // In case the meridiem units are not separated around 12, then implement
        // this function (look at locale/id.js for an example).
        // meridiemHour : function (hour, meridiem) {
        //     return /* 0-23 hour, given meridiem token and hour 1-12 */ ;
        // },
        meridiem : function (hours, minutes, isLower) {
            return hours < 12 ? 'PD' : 'MD';
        },
        week : {
            dow : 1, // Monday is the first day of the week.
            doy : 4  // The week that contains Jan 4th is the first week of the year.
        }
    });
    /** Formato de fecha segun el lenguaje del navegador*/
    moment.locale(userLang.indexOf("en") == 0 ? "en" : userLang.indexOf("fr") == 0 ? "fr" : "es");

});

Uv5kinbx.directive('fileModel', ['$parse', function ($parse) {
    return {
        restrict: 'A',
        link: function (scope, element, attrs) {
            var model = $parse(attrs.fileModel);
            var modelSetter = model.assign;
            element.bind('change', function () {
                scope.$apply(function () {
                    modelSetter(scope, element[0].files[0]);
                });
            });
        }
    };
}]);



/** Rutas de Aplicacion */
var routeDefault = "/";
var routeConfig = "/config";
var routeRadio = "/radio";
var routeTlf = "/tlf";

/** Peticiones REST */
var rest_url_inci = "/inci";
var rest_url_std = "/std";
var rest_url_preconf = "/preconf";
var rest_url_local_config = "/lconfig";
var rest_url_local_config_ext = "/lconfig-ext";
var rest_url_radio_sessions = "/rdsessions";
var rest_url_radio_gestormn = "/gestormn";
var rest_url_radio_gestormn_habilita = "/gestormn/enable";
var rest_url_radio_gestormn_asigna = "/gestormn/assign";
var rest_url_tlf_tifxinfo = "/tifxinfo";
var rest_url_tlf_pbxinfo = "/pbxinfo";
var rest_url_versiones = "/versiones";
var rest_url_radio_hf = "/rdhf";
var rest_url_ps = "/ps";

/** */
var roles = {
	ADMIN_PROFILE: 64,
	ING_PROFILE: 32,
	GEST_POFILE: 16,
	CRTL_PROFILE: 8,
	ALM1_PROFILE: 4,
	ALM2_PROLIFLE: 2,
	VIS_PROFILE: 1
};

var srvtypes = { None: "None", Mixed: "Mixed", Phone: "Phone", Radio: "Radio" }
var states = { Running: "Running", Stopped: "Stopped", Disabled: "Disabled" }
var levels = { Master: "Master", Slave: "Slave", Error: "Error" }


/** */
var routeForUnauthorizedAccess = '/noaut';

/** Validadores. */
var regx_ipval = /^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$/;
var regx_trpval = /^[1-2]+,(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\/[0-9]{2,5}$/;
var regx_atsrango = /^[0-9]{6}-[0-9]{6}$/;
var regx_atsnumber = /^[0-9]{6}$/;
var regx_urlval = /^(http(?:s)?\:\/\/[a-zA-Z0-9]+(?:(?:\.|\-)[a-zA-Z0-9]+)+(?:\:\d+)?(?:\/[\w\-]+)*(?:\/?|\/\w+\.[a-zA-Z]{2,4}(?:\?[\w]+\=[\w\-]+)?)?(?:\&[\w]+\=[\w\-]+)*)$/;
var regx_ipportval = /^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(:[\d]{1,5})?$/;
var regx_urival = /^sip:(.+)@(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(:[\d]{1,5})?$/;
var regx_fid = /^(1|2|3)[0-9]{2}\.[0-9]{2}(0|5)$/;
var regx_fid_vhf = /^(1)(1|2|3)([0-9]{1})\.([0-9])(0|2|5|7)(0|5)$/;   /** 118.000 137.000 */
var regx_fid_uhf = /^(2|3|4)([0-9]{2})\.([0-9])(0|2|5|7)(0|5)$/;      /** 225.000 400.000 */

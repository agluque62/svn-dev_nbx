/** */
angular
    .module('Uv5kinbx')
    .factory('$serv', function ($q, $http) {
        return {
            inci_get: function () {
                return remoteGet(rest_url_inci);
            }
            , stdgen_get: function () {
                return remoteGet(rest_url_std);
            }
            , preconf_list: function () {
                return remoteGet(rest_url_preconf);
            }
            , preconf_delete: function (name) {
                return remoteDel(rest_url_preconf + "/" + name );
            }
            , preconf_activate: function (fecha, name) {
                return remotePost(rest_url_preconf, { fecha: fecha, nombre: name });
            }
            , preconf_saveas: function (fecha, name) {
                return remotePut(rest_url_preconf, { fecha: fecha, nombre: name });
            }
            , lconfig_get: function () {
                return remoteGet(rest_url_local_config);
            }
            , lconfig_ext_get: function () {
                return remoteGet(rest_url_local_config_ext);
            }
            , lconfig_set: function (data) {
                return remotePost(rest_url_local_config, data);
            }
            , lconfig_ext_set: function (data) {
                return remotePost(rest_url_local_config_ext, data);
            }
            , radio_sessions_get: function () {
                return remoteGet(rest_url_radio_sessions);
            }
            , radio_gestormn_get: function () {
                return remoteGet(rest_url_radio_gestormn);
            }
            , radio_hf_get: function () {
                return remoteGet(rest_url_radio_hf);
            }
            , radio_11_get: function () {
                return remoteGet(rest_url_radio_11);
            }
            , radio_11_select: function (data) {
                return remotePost(rest_url_radio_11, data);
            }
            , radio_gestormn_enable: function (data) {
                return remotePost(rest_url_radio_gestormn_habilita, data);
            }
            , radio_gestormn_reset: function () {
                return remoteDel(rest_url_radio_gestormn);
            }
            , radio_gestormn_asigna: function (data) {
                return remotePost(rest_url_radio_gestormn_asigna, data);
            }
            , radio_hf_release: function (data) {
                var url = rest_url_radio_hf + "/" + data.id;
                return remotePost(url, data);
            }
            , tlftifx_info_get: function () {
                return remoteGet(rest_url_tlf_tifxinfo);
            }
            , tlfpbx_info_get: function () {
                return remoteGet(rest_url_tlf_pbxinfo);
            }
            , logs_get: function () {
                return remoteGet("logs/logfile.txt");
            }
            , versiones_get: function () {
                return remoteGet(rest_url_versiones);
            }
            , psinfo_get: function () {
                return remoteGet(rest_url_ps);
            }
        };

        //
        function remoteGet(url) {
            return $http.get(normalizeUrl(url));
        }

        //
        function remotePost(url, data) {
            return $http.post(normalizeUrl(url), data);
            //return $http({
            //    method: 'POST',
            //    url: normalizeUrl(url),
            //    data: data,
            //    username: "some_username_that_doesn't_exist",
            //    password: "any_stupid_password",
            //});
        }

        //
        function remotePut(url, data) {
            return $http.put(normalizeUrl(url), data);
        }

        //
        function remoteDel(url, data) {
            return $http.delete(normalizeUrl(url));
        }

        //
        function normalizeUrl(url) {
            if (Simulate == false)
                return url;
            return "/simulate" + url + ".json";
        }

    });


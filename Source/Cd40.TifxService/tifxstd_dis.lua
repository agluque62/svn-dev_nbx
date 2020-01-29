-- Protocolo TIFX STD
-- declare our protocol
tifxstd_proto = Proto("tifx-std","TIFX-STD")
-- create a function to dissect it
function tifxstd_proto.dissector(buffer , pinfo, tree)

-- En la lista ONLINE	
	pinfo.cols.protocol = "TIFXSTD"
	pinfo.cols.info = buffer(4,36):string().." V"..buffer(44,4):uint().." Rec: "..buffer(40,4):uint()
--	Identificador de Mensaje.
	local subtree    = tree:add(tifxstd_proto, buffer(),"Mensaje TIFX-STD")
	subtree:add(buffer(0,4),  "Tipo     ", buffer(0,4):uint())
	subtree:add(buffer(4,36), "TIFX     ", buffer(4,36):string())
	subtree:add(buffer(44,4), "Version  ", buffer(44,4):uint())
	subtree = subtree:add(buffer(40,4), "Recursos ", buffer(40,4):uint())
	nrec = buffer(40,4):uint()
	for rec=0,nrec-1 do
		index = 48 + 60*rec
		local st_rec = subtree:add(buffer(index, 60), "Recurso " .. buffer(index, 36):string())
--			st_rec:add(buffer(index, 36),  "Recurso    : " .. buffer(index, 36):string())
			st_rec:add(buffer(index+36, 4),"Type       : " .. buffer(index+36, 4):uint())
			st_rec:add(buffer(index+40, 4),"Version    : " .. buffer(index+40, 4):uint())
			st_rec:add(buffer(index+44, 4),"State      : " .. buffer(index+44, 4):uint())
			st_rec:add(buffer(index+48, 4),"Priority   : " .. buffer(index+48, 4):uint())
			st_rec:add(buffer(index+52, 4),"Steps      : " .. buffer(index+52, 4):uint())
			st_rec:add(buffer(index+56, 4),"CallBegin  : " .. buffer(index+56, 4):uint())
		end	

end
-- load the udp.port table
udp_table = DissectorTable.get("udp.port")
-- register our protocol to handle udp port 1001
udp_table:add(1001,tifxstd_proto)

require "session"
require "economy"
require "spin"

-- Điều phối hành động từ client tới các module thư viện trên SaiServer
local action = tostring(payload.action or "spin_start")
if action == "init_player" then
    economy.handle_init_player()
elseif action == "spin_start" then
    spin.handle_spin_start()
elseif action == "spin_stop" then
    spin.handle_spin_stop()
elseif action == "flee" then
    session.handle_flee()
elseif action == "ad_reward" then
    economy.handle_ad_reward()
elseif action == "shop_exchange" then
    economy.handle_shop_exchange()
elseif action == "iap_grant" then
    economy.handle_iap_grant()
else
    output.ok = false
    output.error = "unknown_action: " .. action
end

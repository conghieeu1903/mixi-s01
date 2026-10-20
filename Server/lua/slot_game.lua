-- Gieo lại seed ngẫu nhiên mỗi lần chạy script để tránh sandbox luôn ra cùng một kết quả
local function reseed_random()
    local addr = tostring({}):match("0x(%x+)") or "0"
    local seed = (tonumber(ctx.timestamp) or 0) * 1000003 + (tonumber(addr, 16) or 0) % 1000003
    pcall(math.randomseed, seed)
    math.random()
    math.random()
end
reseed_random()

-- Tính tổng số ô slot theo level (3 x level, tối thiểu 3)
local function get_slot_count(level)
    local lv = math.max(1, math.floor(tonumber(level) or 1))
    return 3 * lv
end

-- Tính chi phí GOLD cho một lượt quay bằng đúng số ô slot
local function get_spin_cost(level)
    return get_slot_count(level)
end

-- Thông số quay cố định, tốc độ vừa để người chơi kịp nhìn và canh nhịp bấm dừng
local SYMBOL_COUNT = 7        -- Mỗi reel có 7 hình: 0..5 là hình thường, 6 là số 7
local SEVEN_ID = 6
local SPIN_SPEED = 5          -- Số hình lướt qua mỗi giây
local SPIN_UP = 0.3           -- Thời gian tăng tốc (giây)
local START_INTERVAL = 0.03   -- Độ trễ khởi động giữa hai reel liền kề
local STOP_INTERVAL = 0.2     -- Độ trễ dừng giữa hai reel liền kề, dừng dần trái sang phải
local AUTO_STOP_SECONDS = 5
local SESSION_SECONDS = 1800
local EARLY_TOLERANCE = 3.0   -- Giây client được phép báo sớm hơn giờ server (bù trễ mạng hai chiều)
local LATE_TOLERANCE = 1.0    -- Giây client được phép báo muộn hơn giờ server (ctx.timestamp làm tròn theo giây)

-- Xác suất số 7 được giữ đúng ô khi canh trúng, còn lại trượt sang ô bên cạnh
local function get_seven_hit_chance(level)
    if level <= 2 then return 0.10 end
    return 0.08
end

-- Quãng đường (số ô) reel đã quay sau active giây, khớp ReelStateBuffer.Distance phía client
local function spin_distance(state, active)
    if active <= 0 then return 0 end
    local speed = tonumber(state.speed) or SPIN_SPEED
    local spin_up = tonumber(state.spin_up) or SPIN_UP
    if active <= spin_up then
        return speed * 0.5 * active * active / spin_up
    end
    return speed * (0.5 * spin_up + (active - spin_up))
end

-- Đọc số nguyên thứ index trong mảng payload, ngoài khoảng lo..hi thì trả nil
local function read_int(list, index, lo, hi)
    if type(list) ~= "table" then return nil end
    local v = tonumber(list[index])
    if v == nil then return nil end
    v = math.floor(v)
    if v < lo or v > hi then return nil end
    return v
end

-- Xáo 7 hình cho một reel, giữ keep_symbol ở mặt keep_face để reel không đổi hình khi bắt đầu quay
local function build_reel_layout(keep_face, keep_symbol)
    local faces = { 0, 1, 2, 3, 4, 5, 6 }
    for i = #faces, 2, -1 do
        local j = math.random(1, i)
        local tmp = faces[i]
        faces[i] = faces[j]
        faces[j] = tmp
    end
    if keep_symbol ~= nil then
        for i = 1, SYMBOL_COUNT do
            if faces[i] == keep_symbol then
                faces[i] = faces[keep_face + 1]
                faces[keep_face + 1] = keep_symbol
                break
            end
        end
    end
    return faces
end

-- Đóng phiên chiến đấu đang mở (nếu có) trước khi tạo phiên mới
local function close_active_session_if_any()
    local cur_id, err = game.battle_session_current_id()
    if err == nil and cur_id ~= nil and cur_id ~= "" then
        return game.battle_session_flee(cur_id)
    end
    return nil
end

-- Khởi tạo vật phẩm ban đầu cho người chơi mới
local function handle_init_player()
    local coin_code = payload.coin_code or "GOLD"
    local starter_coins = math.floor(tonumber(payload.starter_coins) or 0)
    if starter_coins > 0 then
        local coin_def, coin_err = game.get_item_def_by_code(coin_code)
        if coin_err ~= nil or coin_def == nil then
            output.ok = false
            output.error = coin_err or "coin_item_def_not_found"
            return
        end
        local grant_err = game.grant_item(coin_def.id, starter_coins)
        if grant_err ~= nil then
            output.ok = false
            output.error = grant_err
            return
        end
    end

    if payload.grant_generator == true then
        local gen_code = payload.generator_code or "coin_generator"
        local gen_def, gen_err = game.get_item_def_by_code(gen_code)
        if gen_err ~= nil or gen_def == nil then
            output.ok = false
            output.error = gen_err or "generator_item_def_not_found"
            return
        end
        local grant_gen_err = game.grant_item(gen_def.id, 1)
        if grant_gen_err ~= nil then
            output.ok = false
            output.error = grant_gen_err
            return
        end
    end

    output.ok = true
    output.server_time = ctx.timestamp
end

-- Bắt đầu lượt quay: trừ GOLD bằng số slot, xáo hình từng reel, tạo phiên 30 phút và trả quỹ đạo quay cho client
local function handle_spin_start()
    local level = math.max(1, math.floor(tonumber(payload.level) or 1))
    local reel_count = get_slot_count(level)
    local cost = get_spin_cost(level)
    local coin_code = payload.coin_code or "GOLD"
    local coin_def_id = payload.coin_def_id

    if coin_def_id == nil or coin_def_id == "" then
        local coin_def, def_err = game.get_item_def_by_code(coin_code)
        if def_err ~= nil or coin_def == nil then
            output.ok = false
            output.error = def_err or "coin_item_def_not_found"
            return
        end
        coin_def_id = coin_def.id
    end

    local deduct_err = game.deduct_item(coin_def_id, cost)
    if deduct_err ~= nil then
        output.ok = false
        output.error = deduct_err
        return
    end

    if payload.prev_session_id ~= nil and payload.prev_session_id ~= "" then
        local _ = game.battle_session_flee(payload.prev_session_id)
    end

    -- Client gửi mặt (0..6) và hình đang hiện của từng reel; chỉ dùng để giữ hình liền mạch, không ảnh hưởng kết quả
    local layouts = {}
    local start_faces = {}
    for r = 1, reel_count do
        local face = read_int(payload.faces, r, 0, SYMBOL_COUNT - 1) or math.random(0, SYMBOL_COUNT - 1)
        local symbol = read_int(payload.symbols, r, 0, SYMBOL_COUNT - 1)
        layouts[r] = build_reel_layout(face, symbol)
        start_faces[r] = face
    end
    local expires_at = ctx.timestamp + SESSION_SECONDS

    local state = {
        level = level,
        reel_count = reel_count,
        cost = cost,
        speed = SPIN_SPEED,
        spin_up = SPIN_UP,
        start_interval = START_INTERVAL,
        stop_interval = STOP_INTERVAL,
        seven_hit = get_seven_hit_chance(level),
        coin_def_id = coin_def_id,
        layouts = layouts,
        start_faces = start_faces,
        started_at = ctx.timestamp,
        expires_at = expires_at
    }

    local session_id, sess_err = game.battle_session_create(state)
    if sess_err ~= nil or session_id == nil then
        local _ = close_active_session_if_any()
        session_id, sess_err = game.battle_session_create(state)
    end
    if sess_err ~= nil or session_id == nil then
        local _ = game.grant_item(coin_def_id, cost)
        output.ok = false
        output.error = sess_err or "battle_session_create_failed"
        return
    end

    output.ok = true
    output.spin_id = session_id
    output.session_id = session_id
    output.session_expires_at_unix = expires_at
    output.level = level
    output.slot_count = reel_count
    output.cost = cost
    output.speed = SPIN_SPEED
    output.spin_up = SPIN_UP
    output.start_interval = START_INTERVAL
    output.stop_interval = STOP_INTERVAL
    output.auto_stop_seconds = AUTO_STOP_SECONDS
    output.layouts = layouts
    output.start_faces = start_faces
end

-- Tính mặt dừng từng reel theo thời điểm người chơi bấm (dừng dần trái sang phải) và chấm kết quả Bingo
local function handle_spin_stop()
    local session_id = payload.session_id or payload.spin_id
    if session_id == nil or session_id == "" then
        local cur_id, cur_err = game.battle_session_current_id()
        if cur_err ~= nil or cur_id == nil then
            output.ok = false
            output.error = cur_err or "no_active_session"
            return
        end
        session_id = cur_id
    end

    local state, get_err = game.battle_session_get(session_id)
    if get_err ~= nil or state == nil then
        output.ok = false
        output.error = get_err or "session_not_found"
        return
    end

    local started_at = tonumber(state.started_at) or ctx.timestamp
    local expires_at = tonumber(state.expires_at) or (started_at + SESSION_SECONDS)
    if ctx.timestamp >= expires_at then
        local _ = game.battle_session_flee(session_id)
        output.ok = false
        output.error = "session_expired"
        return
    end

    local reel_count = math.max(1, tonumber(state.reel_count) or 3)
    local layouts = state.layouts
    local start_faces = state.start_faces
    if type(layouts) ~= "table" or type(start_faces) ~= "table" then
        local _ = game.battle_session_flee(session_id)
        output.ok = false
        output.error = "invalid_session_state"
        return
    end

    -- Server chỉ có giờ theo giây nên dùng thời điểm bấm client gửi, kẹp trong khung sai số quanh giờ server
    local client_elapsed = (tonumber(payload.elapsed_ms) or 0) / 1000.0
    local server_elapsed = math.max(0, ctx.timestamp - started_at)
    local min_allowed = math.max(0, server_elapsed - EARLY_TOLERANCE)
    local max_allowed = server_elapsed + LATE_TOLERANCE
    local elapsed = math.max(min_allowed, math.min(max_allowed, client_elapsed))

    local start_interval = tonumber(state.start_interval) or START_INTERVAL
    local stop_interval = tonumber(state.stop_interval) or STOP_INTERVAL
    local seven_hit = tonumber(state.seven_hit) or get_seven_hit_chance(tonumber(state.level) or 1)

    -- Thấy gì được nấy: reel r dừng ở ô đang nằm giữa khung lúc (bấm + r * stop_interval); riêng số 7 có thể trượt sang ô bên cạnh
    local faces = {}
    local symbols = {}
    for r = 1, reel_count do
        local i = r - 1
        local layout = layouts[r]
        if type(layout) ~= "table" then layout = { 0, 1, 2, 3, 4, 5, 6 } end
        local active = elapsed + i * stop_interval - i * start_interval
        local pos = (tonumber(start_faces[r]) or 0) + spin_distance(state, active)
        local face = math.floor(pos + 0.5) % SYMBOL_COUNT
        local sym = tonumber(layout[face + 1]) or 0
        if sym == SEVEN_ID and math.random() >= seven_hit then
            local dir = (math.random(0, 1) == 0) and -1 or 1
            face = (face + dir) % SYMBOL_COUNT
            sym = tonumber(layout[face + 1]) or 0
        end
        faces[r] = face
        symbols[r] = sym
    end

    local first_sym = symbols[1] or 0
    local all_match = true
    for r = 2, reel_count do
        if symbols[r] ~= first_sym then
            all_match = false
            break
        end
    end

    output.ok = true
    output.session_id = session_id
    output.reel_index = 0
    output.symbol_id = first_sym
    output.faces = faces
    output.symbols = symbols
    output.elapsed_ms = math.floor(elapsed * 1000 + 0.5)
    output.is_final = true

    local cost = tonumber(state.cost) or get_spin_cost(state.level)
    local outcome = 0
    local reward = 0
    local level_cleared = false

    if all_match then
        level_cleared = true
        if first_sym == 6 then
            outcome = 2
            reward = cost * 70
        else
            outcome = 1
            reward = cost * 3
        end
    end

    if reward > 0 then
        local coin_def_id = state.coin_def_id
        if coin_def_id == nil or coin_def_id == "" then
            local coin_def, def_err = game.get_item_def_by_code(payload.coin_code or "GOLD")
            if def_err ~= nil or coin_def == nil then
                output.ok = false
                output.error = def_err or "coin_item_def_not_found"
                return
            end
            coin_def_id = coin_def.id
        end
        local grant_err = game.grant_item(coin_def_id, reward)
        if grant_err ~= nil then
            output.ok = false
            output.error = grant_err
            return
        end
    end

    local cur_level = tonumber(state.level) or 1
    local next_level = level_cleared and (cur_level + 1) or cur_level

    local end_err = game.battle_session_end(session_id, {
        level = cur_level,
        outcome = outcome,
        reward = reward,
        level_cleared = level_cleared,
        ended_at = ctx.timestamp
    })
    if end_err ~= nil then
        output.ok = false
        output.error = end_err
        return
    end

    output.outcome = outcome
    output.reward = reward
    output.level = next_level
    output.level_cleared = level_cleared
end

-- Hủy phiên quay hiện tại khi hết giờ hoặc thoát màn
local function handle_flee()
    local flee_err = close_active_session_if_any()
    if flee_err ~= nil then
        output.ok = false
        output.error = flee_err
        return
    end
    output.ok = true
end

-- Cộng 100 GOLD khi người chơi hoàn thành xem quảng cáo
local function handle_ad_reward()
    local coin_code = payload.coin_code or "GOLD"
    local reward_amount = 100
    local coin_def, def_err = game.get_item_def_by_code(coin_code)
    if def_err ~= nil or coin_def == nil then
        output.ok = false
        output.error = def_err or "coin_item_def_not_found"
        return
    end

    local grant_err = game.grant_item(coin_def.id, reward_amount)
    if grant_err ~= nil then
        output.ok = false
        output.error = grant_err
        return
    end

    output.ok = true
    output.reward = reward_amount
end

-- Đổi DIAMOND sang GOLD theo gói trong cửa hàng
local function handle_shop_exchange()
    local pack_id = tostring(payload.pack_id or "")
    local packs = {
        coin_pack_small = { diamonds = 10, coins = 500 },
        coin_pack_medium = { diamonds = 50, coins = 3000 },
        coin_pack_large = { diamonds = 100, coins = 7500 }
    }
    local pack = packs[pack_id]
    if pack == nil then
        output.ok = false
        output.error = "invalid_shop_pack"
        return
    end

    local diamond_code = payload.diamond_code or "DIAMOND"
    local coin_code = payload.coin_code or "GOLD"
    local defs, defs_err = game.get_item_defs_by_codes({ diamond_code, coin_code })
    if defs_err ~= nil or defs == nil then
        output.ok = false
        output.error = defs_err or "currency_defs_not_found"
        return
    end

    local diamond_def_id = nil
    local coin_def_id = nil
    for i = 1, #defs do
        if defs[i].item_code == diamond_code then
            diamond_def_id = defs[i].id
        elseif defs[i].item_code == coin_code then
            coin_def_id = defs[i].id
        end
    end

    if diamond_def_id == nil or coin_def_id == nil then
        output.ok = false
        output.error = "currency_defs_missing"
        return
    end

    local deduct_err = game.deduct_item(diamond_def_id, pack.diamonds)
    if deduct_err ~= nil then
        output.ok = false
        output.error = deduct_err
        return
    end

    local grant_err = game.grant_item(coin_def_id, pack.coins)
    if grant_err ~= nil then
        local _ = game.grant_item(diamond_def_id, pack.diamonds)
        output.ok = false
        output.error = grant_err
        return
    end

    output.ok = true
    output.diamonds_spent = pack.diamonds
    output.coins_granted = pack.coins
end

-- Cộng DIAMOND sau khi mua gói IAP thành công
local function handle_iap_grant()
    local product_id = tostring(payload.product_id or "")
    local pack_amounts = {
        diamond_100 = 100,
        diamond_550 = 550,
        diamond_1200 = 1200,
        diamond_2600 = 2600,
        diamond_7000 = 7000
    }
    local amount = pack_amounts[product_id] or math.floor(tonumber(payload.amount) or 0)
    if amount <= 0 then
        output.ok = false
        output.error = "invalid_iap_product"
        return
    end

    local diamond_code = payload.diamond_code or "DIAMOND"
    local diamond_def, def_err = game.get_item_def_by_code(diamond_code)
    if def_err ~= nil or diamond_def == nil then
        output.ok = false
        output.error = def_err or "diamond_item_def_not_found"
        return
    end

    local grant_err = game.grant_item(diamond_def.id, amount)
    if grant_err ~= nil then
        output.ok = false
        output.error = grant_err
        return
    end

    output.ok = true
    output.diamonds_granted = amount
end

-- Điều phối hành động theo payload.action
local action = tostring(payload.action or "spin_start")
if action == "init_player" then
    handle_init_player()
elseif action == "spin_start" then
    handle_spin_start()
elseif action == "spin_stop" then
    handle_spin_stop()
elseif action == "flee" then
    handle_flee()
elseif action == "ad_reward" then
    handle_ad_reward()
elseif action == "shop_exchange" then
    handle_shop_exchange()
elseif action == "iap_grant" then
    handle_iap_grant()
else
    output.ok = false
    output.error = "unknown_action: " .. action
end

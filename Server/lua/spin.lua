-- Bắt đầu lượt quay: trừ GOLD bằng số ô slot, tạo phiên 30 phút và sinh quỹ đạo xoay trên server
function handle_spin_start()
    local level = math.max(1, math.floor(tonumber(payload.level) or 1))
    local max_steps = math.max(1, math.min(32, math.floor(tonumber(payload.max_steps) or 12)))
    local slot_count = 3 * level
    local total_steps = math.min(slot_count, max_steps)
    local cost = slot_count
    local coin_code = payload.coin_code or "GOLD"

    local coin_def, def_err = game.get_item_def_by_code(coin_code)
    if def_err ~= nil or coin_def == nil then
        output.ok = false
        output.error = def_err or "coin_item_def_not_found"
        return
    end

    local deduct_err = game.deduct_item(coin_def.id, cost)
    if deduct_err ~= nil then
        output.ok = false
        output.error = deduct_err
        return
    end

    local cur_id, cur_err = game.battle_session_current_id()
    if cur_err == nil and cur_id ~= nil and cur_id ~= "" then
        local _ = game.battle_session_flee(cur_id)
    end

    local speed = math.min(24, 6 + level * 2)
    local spin_up = 0.3
    local start_interval = total_steps > 1 and math.min(0.12, 0.8 / total_steps) or 0
    local p_hit = math.max(0.35, 0.68 - (level - 1) * 0.05)
    local p_seven = math.max(0.06, 0.12 - (level - 1) * 0.01)
    local auto_stop_seconds = 5

    local layout = { 0, 1, 2, 3, 4, 5, 6 }
    for i = #layout, 2, -1 do
        local j = math.random(1, i)
        local tmp = layout[i]
        layout[i] = layout[j]
        layout[j] = tmp
    end

    local step_start_faces = {}
    for s = 1, total_steps do
        step_start_faces[s] = math.random(0, 6)
    end

    local expires_at = ctx.timestamp + 1800
    local state = {
        level = level,
        slot_count = slot_count,
        total_steps = total_steps,
        cost = cost,
        speed = speed,
        spin_up = spin_up,
        start_interval = start_interval,
        auto_stop_seconds = auto_stop_seconds,
        p_hit = p_hit,
        p_seven = p_seven,
        coin_def_id = coin_def.id,
        layout = layout,
        step_start_faces = step_start_faces,
        stopped_symbols = {},
        stopped_count = 0,
        last_elapsed_sec = 0,
        started_at = ctx.timestamp,
        expires_at = expires_at
    }

    local session_id, sess_err = game.battle_session_create(state)
    if sess_err ~= nil or session_id == nil then
        local _ = game.grant_item(coin_def.id, cost)
        output.ok = false
        output.error = sess_err or "battle_session_create_failed"
        return
    end

    output.ok = true
    output.spin_id = session_id
    output.session_id = session_id
    output.session_expires_at_unix = expires_at
    output.level = level
    output.slot_count = slot_count
    output.total_steps = total_steps
    output.cost = cost
    output.speed = speed
    output.spin_up = spin_up
    output.start_interval = start_interval
    output.auto_stop_seconds = auto_stop_seconds
    output.layouts = layout
    output.step_start_faces = step_start_faces
end

-- Tính góc xoay trên server theo thời gian thực tế, áp dụng độ trượt và chấm Bingo ở bước cuối
function handle_spin_stop()
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
    local expires_at = tonumber(state.expires_at) or (started_at + 1800)
    if ctx.timestamp >= expires_at then
        local _ = game.battle_session_flee(session_id)
        output.ok = false
        output.error = "session_expired"
        return
    end

    local total_steps = math.max(1, tonumber(state.total_steps) or tonumber(state.slot_count) or 3)
    local stopped_count = tonumber(state.stopped_count) or 0
    local reel_index = math.floor(tonumber(payload.reel_index) or stopped_count)
    if reel_index < 0 then reel_index = 0 end
    if reel_index >= total_steps then reel_index = total_steps - 1 end

    if type(state.stopped_symbols) ~= "table" then
        state.stopped_symbols = {}
    end

    local step_key = tostring(reel_index)
    local symbol_id = state.stopped_symbols[step_key]

    if symbol_id == nil then
        local is_auto = (payload.is_auto == true)
        local p_hit = tonumber(state.p_hit) or 0.55
        local p_seven = tonumber(state.p_seven) or 0.10

        local client_elapsed_sec = (tonumber(payload.elapsed_ms) or 0) / 1000.0
        local server_elapsed_sec = math.max(0, ctx.timestamp - started_at)
        local min_allowed = math.max(0.4, server_elapsed_sec - 1.5)
        local max_allowed = server_elapsed_sec + 2.0
        local elapsed_sec = math.max(min_allowed, math.min(max_allowed, client_elapsed_sec))
        local last_elapsed = tonumber(state.last_elapsed_sec) or 0
        if elapsed_sec <= last_elapsed then
            elapsed_sec = last_elapsed + 0.08
        end
        state.last_elapsed_sec = elapsed_sec

        if is_auto then
            if math.random() < p_seven then
                symbol_id = 6
            else
                symbol_id = math.random(0, 5)
            end
        else
            local start_face = 0
            if type(state.step_start_faces) == "table" then
                start_face = tonumber(state.step_start_faces[reel_index + 1]) or 0
            end
            local start_interval = tonumber(state.start_interval) or 0
            local spin_up = tonumber(state.spin_up) or 0.3
            local speed = tonumber(state.speed) or 12
            local active = elapsed_sec - (reel_index * start_interval)
            local distance = 0
            if active > 0 then
                if active <= spin_up then
                    distance = speed * 0.5 * active * active / spin_up
                else
                    distance = speed * (0.5 * spin_up + (active - spin_up))
                end
            end

            local face_count = (type(state.layout) == "table" and #state.layout > 0) and #state.layout or 7
            local current_face = math.floor(start_face + distance + 0.5) % face_count
            local aimed = current_face
            if type(state.layout) == "table" and state.layout[current_face + 1] ~= nil then
                aimed = tonumber(state.layout[current_face + 1]) or current_face
            end

            if math.random() <= p_hit then
                symbol_id = aimed
            else
                local shift = math.random(1, 2)
                local next_face = (current_face + shift) % face_count
                if type(state.layout) == "table" and state.layout[next_face + 1] ~= nil then
                    symbol_id = tonumber(state.layout[next_face + 1]) or ((aimed + shift) % 7)
                else
                    symbol_id = (aimed + shift) % 7
                end
            end
        end

        state.stopped_symbols[step_key] = symbol_id
        stopped_count = stopped_count + 1
        state.stopped_count = stopped_count
    end

    local is_final = (stopped_count >= total_steps)
    output.ok = true
    output.session_id = session_id
    output.reel_index = reel_index
    output.symbol_id = symbol_id
    output.is_final = is_final

    if not is_final then
        local upd_err = game.battle_session_update(session_id, state)
        if upd_err ~= nil then
            output.ok = false
            output.error = upd_err
        end
        return
    end

    local first_sym = tonumber(state.stopped_symbols["0"]) or symbol_id
    local all_match = true
    for s = 0, total_steps - 1 do
        local sym = tonumber(state.stopped_symbols[tostring(s)])
        if sym == nil or sym ~= first_sym then
            all_match = false
            break
        end
    end

    local cur_level = math.max(1, tonumber(state.level) or 1)
    local cost = tonumber(state.cost) or (3 * cur_level)
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

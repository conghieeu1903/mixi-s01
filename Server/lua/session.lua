-- Đóng phiên quay đang mở trên Redis nếu tồn tại
function close_active_session_if_any()
    local cur_id, err = game.battle_session_current_id()
    if err == nil and cur_id ~= nil and cur_id ~= "" then
        local flee_err = game.battle_session_flee(cur_id)
        if flee_err ~= nil then
            return flee_err
        end
    end
    return nil
end

-- Lấy trạng thái phiên quay hiện tại và kiểm tra hạn 30 phút
function load_valid_session(requested_session_id)
    local session_id = requested_session_id
    if session_id == nil or session_id == "" then
        local cur_id, cur_err = game.battle_session_current_id()
        if cur_err ~= nil or cur_id == nil or cur_id == "" then
            return nil, nil, cur_err or "no_active_session"
        end
        session_id = cur_id
    end

    local state, get_err = game.battle_session_get(session_id)
    if get_err ~= nil or state == nil then
        return session_id, nil, get_err or "session_not_found"
    end

    local expires_at = tonumber(state.expires_at) or ((tonumber(state.started_at) or ctx.timestamp) + 1800)
    if ctx.timestamp >= expires_at then
        local flee_err = game.battle_session_flee(session_id)
        if flee_err ~= nil then
            return session_id, nil, flee_err
        end
        return session_id, nil, "session_expired"
    end

    return session_id, state, nil
end

-- Hủy phiên quay hiện tại khi hết giờ hoặc người chơi thoát màn
function handle_flee()
    local flee_err = close_active_session_if_any()
    if flee_err ~= nil then
        output.ok = false
        output.error = flee_err
        return
    end
    output.ok = true
end

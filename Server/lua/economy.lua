-- Khởi tạo xu ban đầu và máy phát xu cho người chơi mới
function handle_init_player()
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

-- Cộng 100 xu khi người chơi xem xong quảng cáo
function handle_ad_reward()
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

-- Đổi kim cương sang xu theo gói trong cửa hàng
function handle_shop_exchange()
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

-- Xác thực và cấp kim cương khi mua gói IAP
function handle_iap_grant()
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

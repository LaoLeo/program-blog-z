require "io"
require "lfs"

local Contents = {}

---拆分字符串
---@param str string 被拆分的源字符串
---@param sep string 拆分的
function string.split(input, delimiter)
    input = tostring(input)
    delimiter = tostring(delimiter)
    if (delimiter=='') then return false end
    local pos,arr = 0, {}
    -- for each divider found
    for st,sp in function() return string.find(input, delimiter, pos, true) end do
        table.insert(arr, string.sub(input, pos, st - 1))
        pos = sp + 1
    end
    table.insert(arr, string.sub(input, pos))
    return arr
end

function urlSpaceEncode(s)
    return string.replace(s, " ", "%20")
end

-- 字符串替换【不执行模式匹配】
-- s       源字符串
-- pattern 匹配字符串
-- repl    替换字符串
--
-- 成功返回替换后的字符串，失败返回源字符串
function string.replace(s, pattern, repl)
    local i,j = string.find(s, pattern, 1, true)
    if i and j then
        local ret = {}
        local start = 1
        while i and j do
            table.insert(ret, string.sub(s, start, i - 1))
            table.insert(ret, repl)
            start = j + 1
            i,j = string.find(s, pattern, start, true)
        end
        table.insert(ret, string.sub(s, start))
        return table.concat(ret)
    end
    return s
end

-- local function urlEncode(s)  
--     s = string.gsub(s, "([^%w%.%- ])", function(c) return string.format("%%%02X", string.byte(c)) end)  
--    return string.gsub(s, " ", "+")  
-- end  

-- local function urlDecode(s)  
--    s = string.gsub(s, '%%(%x%x)', function(h) return string.char(tonumber(h, 16)) end)  
--    return s  
-- end 

function genFilePaths(rootPath, paths, filter)
    paths = paths or {}
    for entry in lfs.dir(rootPath) do
        if entry ~= '.' and entry ~= '..' then
            local path = rootPath .. '/' .. entry
            local attr = lfs.attributes(path)
            assert(type(attr) == 'table')

            if attr.mode == 'directory' then
                genFilePaths(path, paths, filter)
            elseif filter  then
                if filter(path) then
                    table.insert(paths, path)
                end
            else
                table.insert(paths, path)
            end
        end
    end

    return paths
end

function isMdFile(path)
    local ext = path:sub(-2)
    return ext == 'md'
end

function writeFile(fileName, contentTb, mode)
    if fileName==nil then return end

    mode = mode or "w"
    local file = io.open(fileName, mode)
    if contentTb~=nil then
        -- for i,str in ipairs(contentTb) do
        --     file:write(str)
        -- end
        file:write(table.concat(contentTb, "\n"))
    end
    io.flush()
    io.close(file)
end

function pathsFormat(paths, genTitle, genMdLink)
    local dict,record = {},{}
    for _,path in ipairs(paths) do
        local temps = string.split(path, '/')
        local curr,len = dict,#temps
        if not record[temps[2]] and len>2 then
            genTitle(temps[2])
            record[temps[2]] = 1
        end
        
        -- for i=2,len-1 do
        --     -- print(temps[i])
        --     if not curr[temps[i]] then
        --         curr[temps[i]] = {}
        --     end
        --     curr = curr[temps[i]]
        -- end
        genMdLink(temps[len], path)
        -- table.insert(curr, temps[len])
    end
    return dict
end

function genTitle(title)
    -- print("生成Title: ".."###"..title.."\n")
    table.insert(Contents, "### "..title)
end

function genMdLink(title, path)
    -- print("生成Link: "..string.format("[%s](%s)\n", title, path))
    table.insert(Contents, string.format("* [%s](%s)", urlSpaceEncode(title), urlSpaceEncode(path)))
end

function main()
    table.insert(Contents, "# program-blog-z")
    table.insert(Contents, "程序生涯笔记、代码集、博客。")
    table.insert(Contents, "## 目录")

    local rootPath = '.'
    local mdFilePaths = genFilePaths(rootPath, nil, isMdFile)

    pathsFormat(mdFilePaths, genTitle, genMdLink)
    writeFile("README.md", Contents)
end

main()




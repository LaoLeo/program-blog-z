#!/bin/bash

# -----------------------
#  处理参数，得出commitIdArr，moduleNameArr
# -----------------------
isCommitIdParams=0
isModuleNamesParams=0
isExPathParams=0
isOutputFileParams=0
commitIdArr=()
moduleNameArr=()
commitIdArr_index=0
moduleNameArr_index=0
extensionsPath=""
outputFileName=""

for i in "$@"; do
  # echo $i
  if [ $i = "-C" ]; then
    isCommitIdParams=1
    isModuleNamesParams=0
    isExPathParams=0
    isOutputFileParams=0
    continue
  fi
  if [ $i = "-M" ]; then
    isCommitIdParams=0
    isModuleNamesParams=1
    isExPathParams=0
    isOutputFileParams=0
    continue
  fi
  if [ $i = "-P" ]; then
    isCommitIdParams=0
    isModuleNamesParams=0
    isExPathParams=1
    isOutputFileParams=0
    continue
  fi
  if [ $i = "-N" ]; then
    isCommitIdParams=0
    isModuleNamesParams=0
    isExPathParams=0
    isOutputFileParams=1
    continue
  fi
  if [ $isCommitIdParams == 1 ]; then
    commitIdArr[$commitIdArr_index]=$i
    ((commitIdArr_index++))
  fi
  if [ $isModuleNamesParams == 1 ]; then
    moduleNameArr[$moduleNameArr_index]=$i
    ((moduleNameArr_index++))
  fi
  if [ $isExPathParams == 1 ]; then
    extensionsPath=$i
  fi
  if [ $isOutputFileParams == 1 ]; then
    outputFileName=$i
  fi
done

# echo ${commitIdArr[@]} 
# echo "--------------------------"
# echo ${moduleNameArr[@]}


echo "create file"
# FILE=~/Desktop/diff-replace-module-${commitIdArr[0]}-${commitIdArr[1]}.diff
FILE=~/Desktop/${outputFileName}.diff
touch $FILE
echo "" > $FILE

echo "#参数：" >> $FILE
echo "#$*" >> $FILE
echo -e "\n\n" >> $FILE

echo "exporting..."


# ----------------------
# 循环执行git diff 输出到文件
# ----------------------
echo "Files Total: ${#moduleNameArr[@]}"
changedCount=0
for((i=0;i<${#moduleNameArr[@]};i++)) 
do
  name=${moduleNameArr[$i]}
  echo "diff ${name}"
  result=$(git diff ${commitIdArr[@]} "${extensionsPath}**/${name}") 
  if [ -n "$result" ]
  then
    ((changedCount++))
    echo "# ${changedCount} #" >> $FILE
    echo "${name}" >> $FILE
    echo -e "${result}\n\n" >> $FILE
  fi
done
echo "Changed Files Total: ${changedCount}"

sed -i "1iFiles Total: ${#moduleNameArr[@]}\nChanged Files Total: ${changedCount}" $FILE

echo "done"


# --------------------
#  打开文件
# --------------------
# if [ $changedCount -gt 0 ]; then
#     vim $FILE
# fi

# if [ `expr substr $(uname -s) 1 10` = "MINGW64_NT" ]; then
#     C:/Windows/notepad.exe $FILE
# fi
# if [ `uname` = "Darwin" ]; then
#     echo "Max OS X"
# fi
# if [ `expr substr $(uname -s) 1 5` = "Linux" ]; then
#     vim $FILE
# fi
exit 0

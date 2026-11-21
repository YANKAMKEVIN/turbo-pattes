#!/bin/zsh
# Les 10 circuits dans les 3 difficultés (Mac, 1 tour chacun) : échoue si une course ne finit pas, si une anomalie
# ou une exception est signalée, ou si la fluidité moyenne passe sous 30 i/s. Usage : tools/autotest-difficultes.sh [dossier]
out=${1:-/tmp/tp-difficultes}
app=~/dev/turbo-pattes/Builds/macOS/TurboPattes.app/Contents/MacOS/"Turbo Grid"
rm -rf $out && mkdir -p $out
noms=(Facile Normal Difficile)
echec=0
for d in 1 2 3; do
  for n in {1..10}; do
    dir=$out/d$d-c$n
    "$app" -tp-autotest $dir -tp-laps 1 -tp-circuit $n -tp-difficulty $d -logFile $dir.player.log >/dev/null 2>&1
    log=$dir/autotest.log
    fin=$(grep -c "etat=Results" $log)
    anomalies=$(grep -c "ANOMALIE" $log)
    exc=$(grep -c "Exception" $dir.player.log)
    fps=$(grep -o "fps=[0-9]*" $log | cut -d= -f2 | awk '{s+=$1; n++} END {if (n) printf "%d", s/n; else print 0}')
    place=$(grep -o "^  [0-9]\. Alex (joueur)" $log | grep -o "[0-9]" | head -1)
    figures=$(grep -o "figures .*" $log | head -1 | cut -c1-60)
    etat=OK
    if [[ $fin -eq 0 || $anomalies -gt 0 || $fps -lt 30 || $exc -gt 0 ]]; then etat=ECHEC; echec=1; fi
    printf "%-9s %-2d %-6s fps=%-3s joueur=%s/8 anomalies=%s exceptions=%s\n" ${noms[$d]} $n $etat $fps "$place" $anomalies $exc
  done
done
exit $echec

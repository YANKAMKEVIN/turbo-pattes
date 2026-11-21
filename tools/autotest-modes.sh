#!/bin/zsh
# Autotest des modes (Mac) : contre-la-montre (3 tours, records effacés : le fantôme du meilleur tour roule dès le tour 2)
# puis une coupe entière (4 courses d'un tour et podium). Usage : tools/autotest-modes.sh [dossier] [circuit chrono] [coupe]
out=${1:-/tmp/tp-modes}
circuit=${2:-1}
cup=${3:-1}
app=~/dev/turbo-pattes/Builds/macOS/TurboPattes.app/Contents/MacOS/"Turbo Grid"
rm -rf $out && mkdir -p $out
echec=0

"$app" -tp-autotest $out/chrono -tp-mode chrono -tp-fresh -tp-laps 3 -tp-circuit $circuit -logFile $out/chrono.player.log >/dev/null 2>&1
log=$out/chrono/autotest.log
fin=$(grep -c "etat=Results" $log)
vu=$(grep -c "fantome visible" $log)
karts=$(grep -o "karts=[0-9]*" $log | head -1 | cut -d= -f2)
exc=$(grep -c "Exception" $out/chrono.player.log)
etat=OK
if [[ $fin -eq 0 || $vu -eq 0 || $karts -ne 1 || $exc -gt 0 ]]; then etat=ECHEC; echec=1; fi
echo "chrono $etat fin=$fin fantome=$vu karts=$karts exceptions=$exc"
grep "^Chrono\|Tours joueur" $log

"$app" -tp-autotest $out/coupe -tp-mode coupe -tp-cup $cup -tp-laps 1 -logFile $out/coupe.player.log >/dev/null 2>&1
courses=$(ls -d $out/coupe/course* 2>/dev/null | wc -l | tr -d ' ')
exc=$(grep -c "Exception" $out/coupe.player.log)
etat=OK
if [[ $courses -ne 4 || ! -f $out/coupe/podium.png || ! -f $out/coupe/podium.log || $exc -gt 0 ]]; then etat=ECHEC; echec=1; fi
echo "coupe $etat courses=$courses podium=$([[ -f $out/coupe/podium.png ]] && echo oui || echo non) exceptions=$exc"
for n in 1 2 3 4; do grep -h "^Coupe" $out/coupe/course$n/autotest.log 2>/dev/null; done
cat $out/coupe/podium.log 2>/dev/null
exit $echec

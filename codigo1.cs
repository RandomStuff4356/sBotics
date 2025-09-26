//Struct dos sensores de cor
//0 é esquerdo, 1 é central, 2 é direito

List<string> previousActions;

public struct sensorCor
{
	public double r { get; set; }
	public double g { get; set; }
	public double b { get; set; }
	public double lumen { get; set; }
	public string corDominante { get; set; }

	//O "C" no nome dos parâmetros é para indicar que são os valores do construtor
	public sensorCor(double rC, double gC, double bC, double lumenC, string corDominanteC)
	{
		r = rC;
		g = gC;
		b = bC;
		lumen = lumenC;
		corDominante = corDominanteC;
	}
}

//Struct dos motores
//0 é motor direito traseiro
//1 é motor direito frontal
//2 é motor esquerdo traseiro
//3 é motor esquerdo frontal
//4 é motor direito auxiliar
//5 é motor esquerdo auxiliar
//6 é motor da garra esquerda da base
//7 é motor da garra esquerda da ponta
//8 é motor da garra direita da base
//9 é motor da garra direita da ponta
public struct motor
{
	public double currentAngle { get; set; }
	public double targetAngle { get; set; }
	public motor(double currentAngleC, double targetAngleC)
	{
		currentAngle = currentAngleC;
		targetAngle = targetAngleC;
    }
}

//Struct dos motores
//0 é o frontal, 1 é o lateral
public struct ultra
{
	public double leitura { get; set; }
	public double margemDeteccao { get; set; }
	public ultra(double leituraC, double margemDeteccaoC)
	{
		leitura = leituraC;
		margemDeteccao = margemDeteccaoC;
	}
}

int corte = 120, margemDeErro = 10; //Valores de corte e margem de erro para definir a cor dominante

int ciclos = 3;

public sensorCor[] sensoresCor = new sensorCor[3]; //Array para armazenar os sensores de cor
public ultra[] ultras = new ultra[2]; //Array dos ultras
public motor[] motores = new motor[10]; //Array dos motores

//Ultras
UltrasonicSensor ultraFrontal = Bot.GetComponent<UltrasonicSensor>("UltraFren");
UltrasonicSensor ultraLateral = Bot.GetComponent<UltrasonicSensor>("UltraLado");
double margemParede = 3.6;

//Motores
//Servomotor mDirTras = Bot.GetComponent<Servomotor>("motorDirTras");
Servomotor mDirFrente = Bot.GetComponent<Servomotor>("motorDirFrente");
//Servomotor mEsqTras = Bot.GetComponent<Servomotor>("motorEsqTras");
Servomotor mEsqFrente = Bot.GetComponent<Servomotor>("motorEsqFrente");
Servomotor mAuxEsq = Bot.GetComponent<Servomotor>("motorAuxEsq");
Servomotor mAuxDir = Bot.GetComponent<Servomotor>("motorAuxDir");

double speedReta = 200; //Target da reta
double speedCurva = 500; //Target da curva 

//Durações das ações
double length90 = 1;
double lengthFix = 0.05;
double lengthBigFix = 0.18;

//Garra
Servomotor baseGarraEsq = Bot.GetComponent<Servomotor>("baseGarraEsq");
Servomotor pontaGarraEsq = Bot.GetComponent<Servomotor>("pontaGarraEsq");
Servomotor baseGarraDir = Bot.GetComponent<Servomotor>("baseGarraDir");
Servomotor pontaGarraDir = Bot.GetComponent<Servomotor>("pontaGarraDir");
double tempoAbrirGarra = 600;
double tempoLevantarGarra = 2000;
double targetGarra = 800;

//Variável para a inclinação do Bot e o aumento de velocidade providenciado caso a inclinação seja maior do que corteInc
double Inc = Bot.Inclination;
double rampBoost = 400;
double currentRampBoost = 0;
double corteInc = 20;

//Debug
string estado = "indef";
string parteDaPista = "segueLinha";

async Task Main()
{
	await lerSensores();
	IO.OpenConsole();
	await Time.Delay(500);
    await levantarGarra();
	while (true)
	{
		await lerSensores();
		if (preverLadrilho() == "")
		{
			if (parteDaPista == "segueLinha")
			{
				estado = definirEstado();
				if (estado == "vermelho")
				{
					await parar();
					await Time.Delay(7000);
				}

				else if (estado == "cruzada")
				{
					await lopenFrente(speedReta, 1.8);
					await lerSensores();
				}

				else if (estado == "verdeDuplo")
				{
					await lopenEsq(speedCurva, 2.2 * length90);
					lerSensores();
				}

				else if (estado == "verdeDir")
				{
					await lopenFrente(speedReta, lengthBigFix);
					await lopenDir(speedCurva, length90);
					await lopenFrente(speedReta, 1.5);
					await lerSensores();
				}

				else if (estado == "verdeEsq")
				{
					await lopenEsq(speedCurva, length90);
					await lerSensores();
				}

				else if (estado == "90Dir")
				{
					await lopenFrente(speedReta, lengthFix);
					await lopenDir(speedCurva, length90);
					await lerSensores();
				}

				else if (estado == "90Esq")
				{
					await lopenFrente(speedReta, lengthFix);
					await lopenEsq(speedCurva, length90);
					await lerSensores();
				}

				else if (estado == "corrigirDir")
				{
					await lopenDir(speedCurva, lengthFix);
				}

				else if (estado == "corrigirEsq")
				{
					await lopenEsq(speedCurva, lengthFix);
				}

				else
				{
					await lopenFrente(speedReta, 0.1);
				}
				await Time.Delay(20);

				ciclos += 1;

				IO.Print($"Ciclo: {ciclos - 3} | Inclinação: {Inc} | Sensor esquerdo: {sensoresCor[0].corDominante} | Sensor central: {sensoresCor[1].corDominante} | Sensor direito: {sensoresCor[2].corDominante} | Estado: {estado}");
			}
			else
			{
				await abaixarGarra();
				await Time.Delay(100);
				ciclos += 1;
				IO.Print($"Ciclo: {ciclos}");
			}
		}
		else
		{
			if(preverLadrilho() == "zigzag")
			{
				await lopenFrente(speedReta, 1);
			}
        }
		
	}
}

async Task lerSensores()
{
	sensoresCor[0] = new sensorCor
	(
		Bot.GetComponent<ColorSensor>("sensorEsq").Analog.Red,
		Bot.GetComponent<ColorSensor>("sensorEsq").Analog.Green,
		Bot.GetComponent<ColorSensor>("sensorEsq").Analog.Blue,
		Bot.GetComponent<ColorSensor>("sensorEsq").Analog.Brightness,
		definirCorDominante(0) 
	);

	sensoresCor[1] = new sensorCor
	(
		Bot.GetComponent<ColorSensor>("sensorCen").Analog.Red,
		Bot.GetComponent<ColorSensor>("sensorCen").Analog.Green,
		Bot.GetComponent<ColorSensor>("sensorCen").Analog.Blue,
		Bot.GetComponent<ColorSensor>("sensorCen").Analog.Brightness,
		definirCorDominante(1)
	);
	
	sensoresCor[2] = new sensorCor
	(
		Bot.GetComponent<ColorSensor>("sensorDir").Analog.Red,
		Bot.GetComponent<ColorSensor>("sensorDir").Analog.Green,
		Bot.GetComponent<ColorSensor>("sensorDir").Analog.Blue,
		Bot.GetComponent<ColorSensor>("sensorDir").Analog.Brightness,
		definirCorDominante(2)
	);

	ultras[0] = new ultra
	(
		ultraFrontal.Analog,
		margemParede
	);
	
	ultras[1] = new ultra
	(
		ultraLateral.Analog,
		margemParede
    );

    //Motores 0 a 5 são os de locomoção, por isso o targetAngle não importa
    //Motores 6 a 9 são os da garra, o targetAngle é definido na função de cada ação
 //   motores[0] = new motor
	//(
	//	mDirTras.Angle,
	//	0
	//);

	motores[1] = new motor
	(
		mDirFrente.Angle,
		0
	);

	//motores[2] = new motor
	//(
	//	mEsqTras.Angle,
	//	0
	//);

	motores[3] = new motor
	(
		mEsqFrente.Angle,
		0
	);

	motores[4] = new motor
	(
		mAuxDir.Angle,
		0
	);

	motores[5] = new motor
	(
		mAuxEsq.Angle,
		0
	);

	//O targetAngle será definido na funcão
	motores[6] = new motor
	(
		Abs(baseGarraEsq.Angle),
        motores[6].targetAngle
    );

	motores[7] = new motor
	(
		Abs(pontaGarraEsq.Angle),
        motores[7].targetAngle
    );

	motores[8] = new motor
	(
		Abs(baseGarraDir.Angle),
        motores[8].targetAngle
    );

	motores[9] = new motor
	(
		Abs(pontaGarraDir.Angle),
		motores[9].targetAngle
    );

    Inc = Bot.Inclination;
	if (Inc >= corteInc && Inc < 350) currentRampBoost = rampBoost;
	else if (Inc < 0) currentRampBoost = -rampBoost / 2;
	else rampBoost = 0;
}

string definirEstado()
{
	if (sensoresCor[0].corDominante == "azul" && sensoresCor[1].corDominante == "azul" && sensoresCor[2].corDominante == "azul" && ultras[1].leitura < ultras[1].margemDeteccao)
	{
		parteDaPista = "resgate";
		previousActions.Add("Entrou na sala de resgate");
		return "indef";
	}
	else if (sensoresCor[0].corDominante == "vermelho" || sensoresCor[1].corDominante == "vermelho" || sensoresCor[2].corDominante == "vermelho")
	{
		previousActions.Add("Vermelho");
		return "vermelho";//Vermelho do final da pista
	}
	else if (sensoresCor[0].corDominante == "verde" && sensoresCor[2].corDominante == "verde")
	{
		previousActions.Add("Verde duplo");
		return "verdeDuplo";//Beco sem saída
	}
	else if (sensoresCor[0].corDominante == "verde" && sensoresCor[2].corDominante != "verde")
	{
		previousActions.Add("VerdeEsq");
		return "verdeEsq";
		//if (!checou) { definirEstado(); checou = true; return "checando verde";}
		//else {return "verdeEsq";/*Verde esquerdo*/ checou = false; }
	}
	else if (sensoresCor[0].corDominante != "verde" && sensoresCor[2].corDominante == "verde")
	{
		previousActions.Add("VerdeDir");
		return "verdeDir";
		//if (!checou) { definirEstado(); checou = true; return "checando verde";}
		//else { return "verdeDir";/*Verde direito*/checou = false; }
	}
	else if (sensoresCor[0].corDominante == "preto" && sensoresCor[2].corDominante == "branco")
	{
		if (sensoresCor[1].corDominante == "branco")
		{
			previousActions.Add("corrigirEsq");
			return "corrigirEsq";
		}
		else
		{
			previousActions.Add("90Esq");
			return "90Esq";
		}
	}
	else if (sensoresCor[0].corDominante == "branco" && sensoresCor[2].corDominante == "preto")
	{
		if (sensoresCor[1].corDominante == "branco")
		{
			previousActions.Add("corrigirDir");
		    return "corrigirDir";
		}
		else
		{
            previousActions.Add("90Dir");
            return "90Dir";
		}
	}
	else if (sensoresCor[0].corDominante == "preto" && sensoresCor[2].corDominante == "preto")
	{
        previousActions.Add("cruzada");
        return "cruzada";//Interseção
	}
	else
	{
        previousActions.Add("reta");
		return "reta";//Gap ou linha
    }
}


//Target: velocidade que será atingida
//length: duração do movimento em segundos
async Task lopenFrente(double target, double length)
{

	await liberar();

	//mDirTras.Apply(800, target + rampBoost);
	mDirFrente.Apply(800, target + rampBoost);
	//mEsqTras.Apply(800, target + rampBoost);
	mEsqFrente.Apply(800, target + rampBoost);
	mAuxDir.Apply(800, target + rampBoost);
	mAuxEsq.Apply(800, target + rampBoost);

	await Time.Delay(length * 1000);

	await parar();
}

async Task lopenEsq(double target, double length)
{
	await liberar();

	mDirFrente.Apply(800, target + rampBoost);
	//mDirTras.Apply(800, target + 200 + rampBoost);
	mAuxDir.Apply(800, target + rampBoost);
	mEsqFrente.Apply(800, -target * 2 - rampBoost);
	//mEsqTras.Apply(800, -target * 2 - rampBoost);
	mAuxEsq.Apply(800, -target * 2 - rampBoost);

	await Time.Delay(length * 1000);

	await parar();
}

async Task lopenDir(double target, double length)
{
	await liberar();

	mDirFrente.Apply(800, -target * 2 - rampBoost);
	//mDirTras.Apply(800, -target * 2 - rampBoost);
	mAuxDir.Apply(800, -target * 2 - rampBoost);
	mEsqFrente.Apply(800, target + rampBoost);
	//mEsqTras.Apply(800, target + rampBoost);
	mAuxEsq.Apply(800, target + rampBoost);

	await Time.Delay(length * 1000);

	await parar();
}

async Task parar()
{
	//mDirTras.Locked = true;
	mDirFrente.Locked = true;
	//mEsqTras.Locked = true;
	mEsqFrente.Locked = true;
	mAuxEsq.Locked = true;
	mAuxDir.Locked = true;
}
async Task liberar()
{
	//mDirTras.Locked = false;
	mDirFrente.Locked = false;
	//mEsqTras.Locked = false;
	mEsqFrente.Locked = false;
	mAuxDir.Locked = false;
	mAuxEsq.Locked = false;
}

async Task abrirGarra()
{
	motores[7].targetAngle = -98;
	motores[9].targetAngle = -98;
	pontaGarraEsq.Locked = false;
	pontaGarraDir.Locked = false;
	pontaGarraEsq.Apply(400, targetGarra);
	pontaGarraDir.Apply(400, -targetGarra);
	while (motores[7].currentAngle < motores[7].targetAngle)
	{
		lerSensores(); 
		IO.PrintLine($"Ângulo:{motores[7].currentAngle.ToString()}"); 
		await Time.Delay(20);
	}
	pontaGarraEsq.Locked = true;
	pontaGarraDir.Locked = true;
    motores[7].targetAngle = 0;
    motores[9].targetAngle = 0;
}

async Task fecharGarra()
{
    motores[7].targetAngle = 0;
    motores[9].targetAngle = 0;
    pontaGarraEsq.Locked = false;
	pontaGarraDir.Locked = false;
	pontaGarraEsq.Apply(400, -targetGarra);
	pontaGarraDir.Apply(400, targetGarra);
	while (motores[7].currentAngle < motores[7].targetAngle)
	{
		lerSensores();
		IO.PrintLine($"Ângulo:{motores[7].currentAngle.ToString()}"); 
		await Time.Delay(20);
	}
	pontaGarraEsq.Locked = true;
	pontaGarraDir.Locked = true;
}

async Task levantarGarra()
{
    motores[6].targetAngle = 85;
    motores[8].targetAngle = 90;
    baseGarraEsq.Locked = false;
	baseGarraDir.Locked = false;
    baseGarraEsq.Apply(400, -targetGarra);
    baseGarraDir.Apply(400, -targetGarra);
	while (motores[6].currentAngle <= motores[6].targetAngle || motores[8].currentAngle <= motores[8].targetAngle)
    {
		if(motores[6].currentAngle <= motores[6].targetAngle)
		{
	        baseGarraEsq.Apply(400, -targetGarra);
		}
		if (motores[8].currentAngle <= motores[8].targetAngle)
		{
			baseGarraDir.Apply(400, -targetGarra);
		}
        lerSensores(); 
		IO.PrintLine($"Ângulo:{motores[6].currentAngle} | {motores[8].currentAngle}"); 
		await Time.Delay(100);
	}
    baseGarraEsq.Locked = true;
	baseGarraDir.Locked = true;
}
async Task abaixarGarra()
{
    motores[6].targetAngle = 75;
    motores[8].targetAngle = 75;
    baseGarraEsq.Locked = false;
    baseGarraDir.Locked = false;
    baseGarraEsq.Apply(400, -targetGarra);
    baseGarraDir.Apply(400, -targetGarra);
    while (motores[6].currentAngle >= motores[6].targetAngle && motores[8].currentAngle <= motores[8].targetAngle)
    {
        if (motores[6].currentAngle >= motores[6].targetAngle)
        {
            baseGarraEsq.Apply(400, -targetGarra);
        }
        if (motores[8].currentAngle <= motores[8].targetAngle)
        {
            baseGarraDir.Apply(400, -targetGarra);
        }
        lerSensores();
        IO.PrintLine($"Ângulo:{motores[6].currentAngle} | {motores[8].currentAngle}");
        await Time.Delay(100);
    }
    baseGarraEsq.Locked = true;
    baseGarraDir.Locked = true;
}

string definirCorDominante(int indice)
{
	if (sensoresCor[indice].r - sensoresCor[indice].g > margemDeErro) 
	{
		return "vermelho"; 
	}
	else if (sensoresCor[indice].g - sensoresCor[indice].r > margemDeErro) 
	{
		return "verde"; 
	}
	else if (sensoresCor[indice].b - sensoresCor[indice].r > margemDeErro - 3) 
	{
		return "azul"; 
	}
	else if (sensoresCor[indice].g < corte && sensoresCor[indice].r < corte) 
	{
		return "preto"; 
	}
	else if (sensoresCor[indice].g > corte && sensoresCor[indice].r > corte) 
	{ 
		return "branco"; 
	}
	else 
	{
		return ""; 
		IO.PrintLine("Cor dominante não pode ser definida"); 
	}
}

string preverLadrilho()
{
	if (previousActions.Count > 3)
	{
		if (
			previousActions[ciclos - 3] == "corrigirDir" || previousActions[ciclos - 3] == "90Dir" &&
			previousActions[ciclos - 2] == "corrigirEsq" || previousActions[ciclos - 2] == "90Esq" &&
			previousActions[ciclos - 1] == "corrigirDir" || previousActions[ciclos - 1] == "90Dir"
			)
		{
			return "zigzag";
		}
		else return "";
	}
	else return "";
}

double Abs(double x)
{
	if (x < 0) return x * -1;
	else return x;
}
